# Building a database-backed operator

A database backend translates graph access into database queries and returns
Cyphoid rows. Start with [BACKEND_GUIDE.md](BACKEND_GUIDE.md) for the general
backend contract. This guide focuses on node scans, relationship expansion, and
translating pushed WHERE constraints.

## Separate planning from database access

Inherit from `ReferenceOperatorFactory` and implement `BuildNodeScan` and
`BuildExpandAll`. The reference factory supplies filters, projections, aggregation,
sorting, and other generic operators. Override additional hooks when useful.

The factory methods construct operators; they should not open database readers.
Database access belongs in `ExecuteAsync`, so cancellation, resource lifetimes,
and repeated executions are handled correctly.

For a scan, the information flows as follows:

```text
MATCH pattern and WHERE
  -> NodeScanPlan
  -> parameter binding for this execution
  -> BuildNodeScan(variable, label, propertyFilter, ...)
  -> operator stores these arguments
  -> ExecuteAsync creates and executes a database command
  -> candidate nodes become positional rows
```

`PropertyFilter` already contains bound values. The operator does not need to
read `$parameters` or inspect expression delegates to discover restrictions.
Keep connections, commands, readers, and mutable execution state local to each
enumeration. Do not cache an execution's parameter values in shared factory state.

## Choose a storage mapping

For illustration, assume these tables:

```text
nodes(id, kind, country, active)
edges(source_id, target_id, type)
```

Here `kind` represents a single node label. A backend with multiple labels would
use its own label mapping, such as a node-label table. Graph properties need not
be physical columns: a backend can instead map them into JSON or property tables.
The translation must follow that backend's representation of missing properties,
nulls, types, and node identity.

For this query:

```cypher
MATCH (a:Person)
WHERE a.country = 'DK' AND a.active = true
RETURN a
```

`BuildNodeScan` receives label `Person` and a `PropertyFilter` with two conditions:

| PropertyName | Value | Semantics |
| --- | --- | --- |
| country | `MixedValue.String("DK")` | Query |
| active | `MixedValue.Bool(true)` | Query |

If your database comparisons have compatible semantics, the scan can issue:

```sql
SELECT n.id, n.kind, n.country, n.active
FROM nodes AS n
WHERE n.kind = @label
  AND n.country = @p0
  AND n.active = @p1
```

Use command parameters for `Person`, `DK`, and `true`. The parameter syntax shown
is illustrative; adapt it to your database provider. Project everything needed
to hydrate nodes, even when the Cypher RETURN asks for only one property.

## Translate constraints conservatively

Each `PropertyFilterCondition` contains:

- `PropertyName`: the property being tested.
- `Comparison`: Equal, LessThan, LessThanOrEqual, GreaterThan, or GreaterThanOrEqual.
- `Value`: its bound `MixedValue` for this execution.
- `Semantics`: `Pattern` for a pattern property map, or `Query` for a pushed WHERE.

All conditions in a filter are combined with AND. A backend can translate a subset
into database predicates and apply the complete filter locally afterward.

The following is translation pseudocode; `ColumnMapping`, SQL fragments, and
parameter collection are backend-owned types:

```csharp
foreach (var condition in propertyFilter?.Conditions ?? [])
{
    // Resolve identifiers through a trusted mapping, not user-controlled SQL.
    if (!propertyMappings.TryGetValue(condition.PropertyName, out var column))
        continue;

    // This check depends on the column type, match semantics, database collation,
    // missing/null representation, and the MixedValue type of the bound value.
    if (!column.CanTranslateExactly(condition))
        continue;

    var name = AddDatabaseParameter(command, condition.Value);
    // Map the enum to a fixed SQL token; never accept an arbitrary operator string.
    var op = condition.Comparison switch
    {
        PropertyComparison.Equal => "=",
        PropertyComparison.LessThan => "<",
        PropertyComparison.LessThanOrEqual => "<=",
        PropertyComparison.GreaterThan => ">",
        PropertyComparison.GreaterThanOrEqual => ">=",
        _ => throw new ArgumentOutOfRangeException()
    };
    predicates.Add($"{column.TrustedSqlIdentifier} {op} {name}");
}
```

Always parameterize values. Identifiers cannot generally be value parameters;
use fixed mappings or provider-correct identifier quoting. Do not concatenate
`PropertyName`, labels, or property values directly into SQL text.

An SQL predicate may admit extra candidates, but **must not reject any candidate
that `condition.IsMatch` would retain**. Unsupported translations should therefore
be omitted, rather than approximated with a stricter comparison.

Examples of differences to account for:

| Case | Backend responsibility |
| --- | --- |
| Strings | Ensure case, whitespace, and collation rules match the engine. |
| Numeric values | Do not assume SQL coercion matches MixedValue equality. |
| Missing/null properties | Follow the selected match semantics; do not translate every null equality as `= NULL`. |
| Maps | Do not assume database JSON equality reproduces query map equality. |
| Incompatible range types | Retain candidates that would raise runtime errors; translate only when storage types guarantee compatibility. |
| Unknown comparisons | Preserve candidates retained by `IsMatch`, allowing the residual WHERE to evaluate them. |

Use the public matching helpers as the semantic authority:

```csharp
bool accepts = filter.IsMatch(graphNode);

// Or, for one condition while working with database-native property storage:
bool exists = properties.TryGetValue(condition.PropertyName, out var rawValue);
bool acceptsCondition = condition.IsMatch(exists, MixedValue.FromObject(rawValue));
```

Supply `false` and `MixedValue.Null()` for an absent property. Convert database
`DBNull` into a supported null representation before calling MixedValue helpers.
Use `Pattern` matching for pattern-map conditions and `Query` matching for WHERE
conditions; replacing both with `object.Equals` changes behavior.

### Pattern and Query matching rules

`Comparison` selects the operator; `Semantics` selects the matching rules for
**equality**. These are separate choices. The planner supplies them; operators
should preserve them rather than choose a mode based on the property or value.

| Candidate or bound value | Pattern + Equal | Query + Equal |
| --- | --- | --- |
| Property is absent | Reject the candidate. | Compare a null value with the bound value using runtime query equality. |
| Property or bound value is null | Reject the candidate. | Use runtime query equality; do not automatically reject it. |
| Non-null scalar values | Use MixedValue structural equality. | Use runtime query equality. |
| Map values | Use structural equality, including structural equality of null entries. | Use query map equality; retain an unknown result for the residual WHERE. |

For example, with a node that has no `missing` property:

```cypher
MATCH (n {missing: null}) RETURN n
```

The pattern condition rejects that node. In contrast:

```cypher
MATCH (n) WHERE n.missing = null RETURN n
```

Query equality returns null whenever either operand is null, including
`null = null`. WHERE accepts only true, so the second query also returns no rows.
Use `WHERE n.missing IS NULL` to find missing/null properties.

The operator-level distinction is: Pattern rejects these candidates
immediately; Query's `IsMatch` **retains unknown candidates** for the residual
WHERE. For example, `n.missing = 1 AND 1 / 0 = 1` must evaluate the right
operand and report its error. Removing the candidate in SQL would hide that error.
Consequently, a query constraint translated as `column = @value` generally needs
an additional `OR column IS NULL` candidate branch when the column is nullable.
For a null bound value, omit the SQL restriction: every comparison is unknown.
This applies to both equality and ranges; it is not the final WHERE result.

Maps need a similar distinction: structurally equal maps containing null entries
can match a pattern equality, while query map equality can return unknown. The
candidate filter retains unknown results. The residual WHERE uses three-valued
logic to determine the result and evaluates later operands when required. A pushed
constraint is therefore a candidate restriction, not a replacement for WHERE.

For `<`, `<=`, `>`, and `>=`, both semantics modes use the **same runtime
comparison rules**, including null propagation (not sorting's null ordering).
Pattern maps generate only Equal constraints. Range matching retains unknown
candidates and candidates whose comparison raises a RuntimeException, so the original
WHERE can report that error at its source location. A backend must not silently
remove incompatible values by applying a stricter SQL type filter or conversion.

An operator applying constraints locally needs no manual mode switch: call
`condition.IsMatch(exists, value)` or `filter.IsMatch(node)` as shown above.
A database translator must check both `Comparison` and `Semantics`, along with
its storage types and collation. If it cannot reproduce the required candidate
set, leave that condition out of SQL and use the local helper after retrieval.

The planner extracts a safe leading sequence of single-property
comparisons (`=`, `<`, `<=`, `>`, `>=`) against scalar literals or parameters.
`PropertyFilterCondition.Comparison` identifies the operation and defaults to
`Equal`. Reverse the database comparison only when required by your own storage
mapping; the planner has already normalized reversed Cypher operands.
It stops at unsupported terms, after a range, and after parameter/null equalities.
OR, functions, EXISTS, and comparisons
between variables are evaluated by the residual WHERE. The residual evaluates
pushed comparisons too. A backend must enforce pattern-map conditions itself.

## Implement a scan operator

This example is an operator skeleton. `IDatabaseNodeStore` is an illustrative
backend-owned abstraction, not a Cyphoid API. Its implementation translates the
safe restrictions into database commands and returns fully hydrated `IGraphNode`
instances. The scan applies the full local filter even if the store translated
only some restrictions.

```csharp
using System.Runtime.CompilerServices;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

public interface IDatabaseNodeStore
{
    IAsyncEnumerable<IGraphNode> ReadCandidatesAsync(
        string? label,
        PropertyFilter? restrictions,
        string? targetRelationshipType,
        CancellationToken ct);
}

public sealed class DatabaseNodeScanOperator(
    IDatabaseNodeStore store,
    VariableDefinition variable,
    string? label,
    PropertyFilter? propertyFilter,
    string? targetRelationshipType,
    RowSchema matchColumns) : IOperator
{
    public async IAsyncEnumerable<IRow> ExecuteAsync(
        IQueryContext context,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await foreach (var node in store.ReadCandidatesAsync(
            label, propertyFilter, targetRelationshipType, ct)
            .WithCancellation(ct))
        {
            ct.ThrowIfCancellationRequested();
            if (propertyFilter != null && !propertyFilter.IsMatch(node))
                continue;

            var row = context.RowFactory.EmptyRow(matchColumns);
            row.Values[variable.SlotIndex] = MixedValue.GraphNode(node);
            yield return row;
        }
    }
}
```

Wire this into the factory's `BuildNodeScan` override by passing its arguments
unchanged into the constructor. The store must enforce the label and, when
provided, `targetRelationshipType`. For the illustrated schema, the latter can
be expressed as an outgoing-edge existence check:

```sql
AND EXISTS (
  SELECT 1 FROM edges AS e
  WHERE e.source_id = n.id AND e.type = @relationshipType
)
```

When there is no property filter, do not add property restrictions. A label of
null means no label restriction. Reuse the exact `matchColumns` instance for every
output row; only the values array and host metadata are per-row allocations.

## Execute database commands and hydrate nodes

Inside the store implementation:

1. Obtain a connection from an execution-scoped connection provider or pool.
2. Build a parameterized command from the supported restrictions.
3. Execute it asynchronously and pass `ct` to connection, command, and reader I/O.
4. Convert database values into the backend's supported graph-property values.
5. Return graph nodes containing their IDs, properties, and required relationships.
6. Dispose owned readers, commands, and connections on completion, errors,
   cancellation, and early disposal by consumers such as LIMIT or EXISTS.

Use `await using` for owned resources that support asynchronous disposal. Never
dispose a host-owned connection or transaction unless ownership was transferred.
Preserve the primary query/cancellation exception if cleanup also fails, following
[ERROR_HANDLING.md](ERROR_HANDLING.md).

`IGraphNode` includes identity, `Type`, `Attributes`, and `Edges`. Populate them
consistently with your expansion implementation. In particular, reference
ExpandInto checks the source node's edges; returning an empty edge list merely
because the initial scan did not fetch edges would produce incorrect results.
`GraphNode<TId>` in the reference backend is available, or implement `IGraphNode`
yourself. Ensure IDs have a stable supported MixedValue representation.

The SQL above only shows scalar node columns. Fetch adjacency data using a join,
a batched second query, or another backend-specific hydration strategy. Avoid
opening nested commands on an active reader unless the provider supports that
usage. For large results, hydrate bounded batches rather than buffering the whole
graph or issuing one adjacency query per node. If overriding ExpandInto to query
the database, keep the node/edge contract consistent with all other consumers.

## Implement ExpandAll

`BuildExpandAll` receives the input stream, source and destination variables,
direction, relationship type, destination label, destination property filter,
and shared output schema.

For each input row:

1. Read `row.Values[sourceVariable.SlotIndex]`. Skip a null source.
2. Get its backend node ID and query matching relationships.
3. Apply safely translated label/property restrictions to the destination query.
4. Hydrate destination nodes and apply the complete destination filter locally.
5. Create `row.CopyAndExpand(matchColumns)`, fill the destination slot, and emit it.

Copy through the host row hook to preserve existing values and metadata. Do not
construct an unrelated empty row for each destination.

For an outgoing expansion, the database query has this general shape:

```sql
SELECT d.id, d.kind, d.country, d.active
FROM edges AS e
JOIN nodes AS d ON d.id = e.target_id
WHERE e.source_id = @sourceId
  AND e.type = @relationshipType
  AND d.kind = @destinationLabel
  AND d.country = @country
```

Only add the optional type, label, and property restrictions when present and
safely translatable. Incoming expansion reverses source/target roles; undirected
expansion must cover both orientations. Follow Cyphoid's expansion
behavior for duplicate targets and self-loops; avoid introducing extra duplicates
through hydration joins. See the test backend's ExpandAll implementation.

Start with one lookup per input row, then consider batching source IDs or
caching immutable adjacency within an execution. Preserve each input row's
multiplicity, bindings, and metadata when mapping batched results back to rows.

Do not emit an optional fallback yourself. OPTIONAL MATCH's enclosing operator
handles the complete pattern, its WHERE, and the null fallback. Expansion operators
emit actual matches only.

## Verify the backend

Compare database results with the test/reference backend for the same graph and
queries. Include pattern and query equality, missing/null properties, maps,
string case, numeric types, incoming/undirected expansion, duplicate targets,
self-loops, repeated input rows, and OPTIONAL MATCH fallback. Check that early
exit and cancellation close readers and release connections, that metadata
survives expansion, and that output rows retain the supplied schema instance.

Inspect generated SQL and database execution plans separately to confirm useful
indexes are selected. Correct results alone do not establish that the database
applied the restrictions efficiently.


## SKIP and database offsets

Cyphoid executes SKIP in the reference row pipeline. Do not translate a query's
SKIP into an offset on a node scan or individual expansion. Filtering, expansion,
aggregation, DISTINCT, and sorting determine the result stream before SKIP runs;
a database offset at an earlier stage can discard different rows. No SKIP value
is supplied to scan or expansion operators.
