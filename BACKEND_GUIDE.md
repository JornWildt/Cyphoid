# Backend Implementation Guide

This document explains how to implement a backend for Cyphoid by providing an `IOperatorFactory` and a small set of data-access operators. The easiest path is to inherit from `ReferenceOperatorFactory`, which supplies all generic operators (filter, projection, aggregation, order by, etc.) using `IAsyncEnumerable` row pipelines. You then implement the few abstract methods that perform real graph lookups.

For database query translation, resource management, and an operator example, see
[Building a database-backed operator](DATABASE_BACKEND.md).

## Key concepts
- **Rows are positional**: each variable has a fixed `SlotIndex` (see `VariableDefinition`), and operators write values into `IRow.Values[slot]`.
- **Schemas are shared**: `IRow.Schema` references an immutable `RowSchema`. `IRow.Columns` is a read-only view of that schema, shared by all rows in the stream rather than copied into every row.
- **Values are `MixedValue`**: nodes and properties are transported through rows as `MixedValue` instances.
- **Operators are async streams**: every `IOperator` exposes `IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context, CancellationToken ct = default)`.

## Recommended approach
`BuildExpandAll` and `BuildExpandInto` emit only matches. The enclosing clause-level optional-match operator preserves an incoming row with null new bindings when the whole pattern and WHERE have no matches; backend expansion operators must not supply their own fallback rows.

Graph nodes implement the non-generic `IGraphNode` interface, including `MixedValue GetId()`. The scalar function `id(node)` returns this value and `id(null)` returns null. IDs retain their backend-defined query type, such as string or integer. Relationship IDs are not supported.

`GraphNode<TId>.GetId()` uses `MixedValue.FromObject(Id)`. If your ID type is not supported by that conversion (for example, `Guid`), override `GetId()` to choose a supported query representation, such as `MixedValue.String(Id.ToString())`.

Create a concrete operator factory by inheriting from `ReferenceOperatorFactory` and implementing the abstract methods. These are the only required backend hooks.

Required overrides:
- `BuildNodeScan(...)` - fetch initial nodes for `MATCH` patterns.
- `BuildExpandAll(...)` - expand relationships from a bound source node to a new (unbound) destination.

You can see a working example in `Src/Cyphoid.Tests/TestBackend/TestOperatorFactory.cs` and the test operators under `Src/Cyphoid.Tests/TestBackend/Operators/`.

## Minimal skeleton
```csharp
internal sealed class MyOperatorFactory : ReferenceOperatorFactory
{
  protected override IOperator BuildNodeScan(
    VariableDefinition variable,
    string? label,
    PropertyFilter? propertyFilter,
    string? targetRelationshipType,
    RowSchema matchColumns)
    => new MyNodeScanOperator(...);

  protected override IOperator BuildExpandAll(
    IOperator input,
    VariableDefinition sourceVariable,
    ExpandDirectionType direction,
    string? relationshipType,
    VariableDefinition destinationVariable,
    string? destinationLabel,
    PropertyFilter? destinationPropertyFilter,
    RowSchema matchColumns)
    => new MyExpandAllOperator(...);
}
```

## Operator responsibilities

### Row creation
- Supply an `IQueryRowFactory` through `IQueryContext.RowFactory`; row creation is separate from the operator factory.
- Implement both `EmptyRow(RowSchema columns)` and `EmptyRow(IRow source, RowSchema columns)`. The second overload can preserve backend metadata from a source row.
- Return an `IRow` with the supplied columns and enough values for every declared variable slot. The reference `Row` class is available if no custom row implementation is needed.
- Retain the exact `RowSchema` supplied to your row factory. Do not reconstruct or copy schemas per row. Only the value array and host metadata belong to each row.

### Immutable schemas

`RowSchema` snapshots column definitions once, including the fields of mutable
host `IRowColumn` implementations. Its columns have sequential zero-based indexes,
unique names, and immutable Index/Name/Type values. It exposes Count, an indexer,
and read-only enumeration; `RowSchema.Empty` is shared for the initial empty row.

```csharp
var schema = new RowSchema(new IRowColumn[]
{
  new RowColumn(0, "name", MixedValue.ValueType.String)
});
IRow first = new Row(schema);
IRow second = new Row(schema);
// ReferenceEquals(first.Schema, second.Schema) is true.
// Their Values arrays are separate.
```

Row factories, operator factories, row copies, and procedure output schemas use
`RowSchema`. Custom rows implement `IRow.Schema`; `IRow.Columns` provides a default
read-only view of that schema. `Columns.Count` gives the column count. Overrides
of `Row.CreateEmptyRow` and `CopyAndExpand` accept `RowSchema` and pass the supplied
instance to the output row.

The reference backend creates schemas at parsing or operator construction time.
Projection creates a schema for its new columns; expansion and optional fallback
reuse their supplied match schema. Procedure local-output schemas are created
once per call operator and reused across invocations. UNION normalizes branch
rows to the first emitted schema through the host row-copy hook, preserving
values and metadata. No global schema cache is needed.

Column Type is declared/inferred metadata; values may differ in kind,
especially across UNION branches. Inspect `row.Values[i]?.Type` for the runtime
value type. Maps with different keys do not require different row schemas.

### Node scan (BuildNodeScan)
- Produce one row per matching node in your storage layer.
- Apply `label` and `propertyFilter` when provided.
- `targetRelationshipType` is a hint for the following relationship expansion; any optimization using it must preserve the scan's matching semantics.
- For each match, set `row.Values[variable.SlotIndex]` to a `MixedValue` that wraps your graph node.
- Use `matchColumns` when constructing the row so downstream operators can access the correct metadata.

### Expand all (BuildExpandAll)
- Read the bound source node from `sourceVariable.SlotIndex` in each input row.
- Traverse relationships based on:
  - `direction` (incoming/outgoing)
  - `relationshipType` (relationship type)
  - `destinationLabel` (node label)
  - `destinationPropertyFilter` (node properties)
- For each match, emit a new row where `destinationVariable.SlotIndex` is filled with the destination node value.
- Preserve existing row values (copy/clone if your row type is mutable).

## Optional overrides (performance)
`ReferenceOperatorFactory` implements these generically, but you can override them for better performance or pushdowns:
- `BuildExpandInto` (relationship check between already-bound nodes)
- `BuildFilter` (WHERE)
- `BuildProjection`, `BuildDistinct`, and `BuildAggregateProjection` (the last implements `IOperatorFactory.BuildAggregationProjection`)
- `BuildOrderBy`, `BuildLimit`, `BuildCartesianProduct`

If you override, preserve semantics and output row shapes exactly.

## Async behavior and correctness
- EXISTS uses normal backend scan/expansion operators and stops enumeration at the first result. Backends must dispose resources on early exit and treat null bound source nodes as having no expansions. Async subquery predicates use a reference async filter; projection, grouping, and sorting can receive rows with internal materialized expression slots before projecting the public result schema.
- `BuildExecutionPlan` compiles a reusable template. Backend factory hooks are invoked when execution binds parameter values, not necessarily while the template is built. Each execution receives fresh mutable engine operators/aggregate states and shared schemas. Scan/expansion property filters and limits are already concrete values when passed to backend hooks.
- Projection hooks may receive `ProjectionBindings`, whose `Schema` retains the template's immutable output schema. Reuse it in custom projection operators rather than reconstructing schemas per execution.
- Backend operators implement `ExecuteAsync(IQueryContext context, CancellationToken ct = default)`. Callers can omit the token when cancellation is not needed.
- Mark async iterator token parameters with `[EnumeratorCancellation]` (from `System.Runtime.CompilerServices`) so `.WithCancellation(ct)` works as well as direct token arguments.
- Pass `ct` to upstream `ExecuteAsync(context, ct)` calls and asynchronous storage operations. Enumerate upstream streams with `.WithCancellation(ct)`. Check `ct.ThrowIfCancellationRequested()` before work and periodically inside CPU loops, including loops that discard rows.
- Cancellation must propagate as `OperationCanceledException`; do not wrap it as a backend error. Dispose enumerators and release resources through `await using` or iterator `finally` blocks.
- `ExecuteAsync` should be a true async stream. If you have no async work, a single `await Task.Yield()` keeps the method async.
- Do not block on I/O; use async DB drivers where possible.
- Ensure your operators are deterministic for the same input rows.

## Example implementation
The test backend shows a complete minimal implementation:
- Factory: `Src/Cyphoid.Tests/TestBackend/TestOperatorFactory.cs`
- Node scan: `Src/Cyphoid.Tests/TestBackend/Operators/NodeScanTestOperator.cs`
- Graph store: `Src/Cyphoid.Tests/TestBackend/InMemoryGraph.cs`


## WHERE constraints in MATCH operators

The planner can pass leading property comparisons from a MATCH's WHERE into
`BuildNodeScan`, `BuildExpandAll`, and `BuildExpandInto`. These arrive in the
`PropertyFilter`, alongside constraints from pattern property maps.
Each condition names its property and carries a `MixedValue` bound for this
execution; the backend does not need to inspect a compiled expression delegate.

For example:

```cypher
MATCH (a:Person)-[:KNOWS]->(b:Person)
WHERE a.country = 'DK' AND b.active = true
RETURN a, b
```

The scan receives `country = 'DK'`; the expansion receives `active = true` for
its destination node. Filtering scan candidates before constructing rows avoids
expanding rejected people. Filtering destination candidates avoids emitting
rejected neighbors. WHERE also executes as a residual filter.

**Backend implementations must respect `PropertyFilterCondition.Semantics`.**
`Pattern` retains pattern-map matching; `Query` uses the runtime's query equality.
Use `condition.IsMatch(propertyExists, propertyValue)` for each condition, or
`filter.IsMatch(graphNode)` when working with `IGraphNode`. Supply
`MixedValue.Null()` for a missing property and pass `false` for `propertyExists`.
Do not replace this with `object.Equals`, or automatically reject missing/null
properties: query equality and pattern-map matching have different semantics.
Query constraints retain unknown map comparisons so the residual evaluator can
report source-located errors. An index may implement the constraint only if it
retains all these candidates; otherwise apply `IsMatch` to a broader candidate set.

For equality, Pattern rejects missing/null properties and null bound values;
Query compares missing properties as null and preserves runtime query equality,
including unknown scalar and map comparisons. Any equality or range comparison
with null evaluates to null, including `null = null`; use `IS NULL` to test
for missing/null values. Query candidate filters retain unknown results so later
WHERE operands are evaluated when required. For range comparisons both modes use the same runtime
comparison rules, retaining null and incompatible values for residual evaluation.
See [Pattern and Query matching rules](DATABASE_BACKEND.md#pattern-and-query-matching-rules)
for the behavior table, examples, and database translation requirements.

Pushdown stays within one MATCH, including OPTIONAL MATCH's candidate branch;
null fallback occurs after the branch and its WHERE. The planner uses actual node
bindings rather than assuming every schema slot is populated. Independent pattern
parts receive their own constraints before their Cartesian product.

The planner extracts a leading sequence of
single-property comparisons (`=`, `<`, `<=`, `>`, `>=`) against scalar literals or
parameters, accepting either operand order and parentheses. `Comparison` identifies
the operation; equality is the default. It stops at the first
unsupported term, after a range, and after a parameter or null equality. Range
matching retains incompatible values for the source-located residual error; later
constraints cannot be pushed ahead of that possible error. OR, functions,
EXISTS, nested property lookups, and comparisons between variables are evaluated by the residual filter.
This prevents moving a later condition ahead of a possible error or changing how
often a volatile expression runs. Backend indexes must preserve the runtime
comparison semantics.


### Null handling in host operators

NodeScan and ExpandAll implementations must use
`PropertyFilterCondition.IsMatch` / `PropertyFilter.IsMatch`, or equivalent
candidate semantics. Pattern-map equality rejects missing/null properties.
Query equality and ranges retain unknown candidates; the residual WHERE keeps
only true and may need to evaluate further operands when an earlier one is null.
Null range operands produce unknown. Do not drop unknown
candidates just because SQL WHERE would drop them. Translate nullable query
comparisons using a broader candidate predicate (for example,
`age >= @minAge OR age IS NULL`), or omit that SQL predicate and filter locally.
For a null bound value, leave query candidates unrestricted. Retain incompatible
types for source-located runtime errors. See DATABASE_BACKEND.md for
translation details.
