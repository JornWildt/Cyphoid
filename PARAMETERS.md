# Query parameters

Parameters supply values separately from query text. Parse and build a query
once, then execute it with different values:

```cypher
MATCH (n:Person)
WHERE n.age >= $minimumAge
RETURN n.name
LIMIT $maximumResults
```

```csharp
ICypherParser parser = new CypherAstParser();
var query = parser.ParseQuery(queryText);
var execution = query.BuildQueryPlan().BuildExecutionPlan(operatorFactory);

var parameters = new QueryParameters(new Dictionary<string, MixedValue>
{
    ["minimumAge"] = MixedValue.Int(18),
    ["maximumResults"] = MixedValue.Int(10)
});
var context = new MyQueryContext(rowFactory, parameters);
await foreach (var row in execution.ExecuteAsync(context, ct))
{
    // Consume rows.
}
```

`QueryParameters` is in `Cyphoid.Core.Execution`; `MixedValue` is in
`Cyphoid.Core.Expressions`. A host context can expose the values as follows:

```csharp
public sealed class MyQueryContext(
    IQueryRowFactory rowFactory,
    QueryParameters parameters) : IQueryContext
{
    public IQueryRowFactory RowFactory { get; } = rowFactory;
    public IReadOnlyDictionary<string, MixedValue> Parameters { get; } = parameters;
}
```

Existing contexts need no changes: `IQueryContext.Parameters` defaults to
`QueryParameters.Empty`.

## Names, scope, and values

- Names are ordinal and case-sensitive.
- This subset accepts a letter or underscore followed by letters, digits, or underscores.
  Parameter names can be keywords, such as `$limit`. Numeric-only and escaped
  parameter names are not supported.
- Parameters are available throughout the execution, across WITH and all UNION
  branches. They are separate from row variables and consume no row-schema slots.
- `query.RequiredParameters` exposes unique required names in source order.
- Missing parameters raise a located `RuntimeException` with code
  `MissingParameter`, before backend operator creation or result consumption.
  This also applies to branches returning no rows, LIMIT 0, and arguments that
  would otherwise be skipped by lazy coalesce evaluation.
- Supplying `MixedValue.Null()` is different from omitting a parameter. Extra
  parameters are ignored. Runtime operations validate the supplied value types.

All current MixedValue types can be supplied, including nested maps. No values
are interpolated into the query text, and PrettyPrint retains the placeholders.

## Supported positions

Parameters can be used in scalar expressions, map literals and projections,
property lookup, grouping expressions, ORDER BY, and aggregate arguments:

```cypher
MATCH (n:Person)
WHERE n[$propertyName] >= $minimum
RETURN {name: n.name, configuration: $config} AS result
```

Node pattern property filters accept literal or parameter values:

```cypher
MATCH (n:Person {name: $name})
RETURN n
```

These filters are bound once for execution and passed as concrete values to
backend scan and expansion hooks. Null property filters do not match nodes.
Arbitrary row-dependent expressions in pattern property maps remain unsupported.

WITH and RETURN LIMIT accept an integer literal or a single parameter:

```cypher
MATCH (n:Person)
WITH n ORDER BY n.age LIMIT $candidateCount
RETURN n.name LIMIT $resultCount
```

The supplied limit must be an integer from 0 to Int32.MaxValue. Nulls, strings,
doubles, negative integers, and oversized integers produce located runtime
errors. General LIMIT expressions are not supported.

Labels, relationship types, procedure names, and aliases remain fixed query
structure. Use `n[$propertyName]` for a dynamic property key, rather than
`n.$propertyName`.

## Procedure configuration

An installed procedure can declare a Map parameter and receive a configuration
object directly:

```cypher
CALL analytics.pageRank($config)
YIELD node, score
RETURN node.name, score
```

```csharp
var parameters = new QueryParameters(new Dictionary<string, MixedValue>
{
    ["config"] = MixedValue.Map(new Dictionary<string, MixedValue>
    {
        ["iterations"] = MixedValue.Int(20),
        ["dampingFactor"] = MixedValue.Double(0.85)
    })
});
```

The procedure reads `arguments[0].AsMap()` as usual. Its schema checks,
cancellation, and host metadata handling are unchanged. The original host
context is passed through without a wrapper, so host-specific context types
and query resources remain accessible.

## Binding and plan reuse

`BuildExecutionPlan` compiles a reusable binding template. Each enumeration of
`ExecuteAsync` snapshots the supplied parameter dictionary, checks required names,
and creates execution-local evaluators/operators. Bound parameter values are
captured in row evaluators, avoiding parameter-dictionary lookups per row.
Parameter values and mutable aggregate states are never stored on the shared
AST or template. Row schemas remain shared between executions.

`QueryParameters` already owns an immutable snapshot, so execution can reuse it
without copying it again. If a host supplies a mutable dictionary, bindings use
the snapshot taken when enumeration starts. Do not mutate a dictionary while
that snapshot is being copied. Plugins reading `context.Parameters` directly
still see the original host property; supplying QueryParameters keeps that view
immutable too.

Concurrent reuse has isolated engine parameter bindings and aggregation state.
Host operator factories, graph storage, and plugin implementations must also be
safe for concurrent execution if the host chooses to execute queries concurrently.
