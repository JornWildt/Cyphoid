# Implementation summary

The Cypher engine is implemented as a pipeline of row-oriented physical operators. MATCH clauses extend the current row stream by pattern matching, WHERE filters rows, and WITH/RETURN perform projection. Aggregation is handled as part of projection: if a projection contains aggregate expressions, all non-aggregate expressions become grouping keys. The engine detects aggregates during AST analysis, then evaluates grouping keys per input row, buckets rows by key, and computes aggregate states such as count, min, and max. Pattern expansion distinguishes between Expand(All) for binding new variables and Expand(Into) for checking relationships between already bound variables. Runtime rows are represented in positional form with column metadata, allowing expressions to be compiled into efficient evaluators rather than interpreted through repeated dictionary lookups.

## Supported Cypher subset

- Clauses: MATCH, WHERE, WITH, RETURN, ORDER BY, LIMIT, and CALL ... YIELD with an optional WHERE.
- Procedure plugins are installed with `ICypherParser.InstallPlugin`. See `PROCEDURES.md` for the public contracts and supported subset. Procedures return local-schema host rows; the reference call operator maps YIELD fields into query slots and uses `IRow.MergeWith` to preserve host metadata.
- Combined queries: UNION (also UNION DISTINCT) removes duplicate output rows; UNION ALL retains them. Branches have independent scopes and must return matching column names in matching order. Mixed distinct/ALL chains are rejected.
- Patterns: node patterns with optional labels and property maps; relationship patterns with direction and optional type.
- Expressions: boolean logic, comparisons, IN list, property access, COUNT(*) and basic function calls.
- Aggregation: grouping is implicit when any projection contains aggregate expressions.

## Execution model

- Pipeline is `IAsyncEnumerable` of rows; operators implement `IOperator.ExecuteAsync`.
- `ExecuteAsync(IQueryContext context, CancellationToken ct = default)` propagates cancellation end to end. Async iterators use `[EnumeratorCancellation]`, pass `ct` upstream and to I/O, and check cancellation during CPU work. Name cancellation-token parameters `ct`.
- Rows are positional; each variable binds to a fixed `SlotIndex` in `IRow.Values`.
- Expressions compile into evaluators, not runtime dictionary lookups.
- Query errors use public `ParseException`/`RuntimeException` with `CypherErrorCode`, phase, and optional source spans. Planning and nested expressions use `CompileEvaluator` to locate runtime failures. Preserve cancellation and primary failures during cleanup; see `ERROR_HANDLING.md`.

## Backend contract

- Backends implement `IOperatorFactory`; the recommended path is to inherit `ReferenceOperatorFactory`.
- Required overrides: `BuildNodeScan`, `BuildExpandAll`. Row creation is provided through `IQueryContext.RowFactory` implementing `IQueryRowFactory`.
- Optional overrides: `BuildExpandInto`, `BuildFilter`, `BuildProjection`, `BuildDistinct`, `BuildAggregateProjection`, `BuildOrderBy`, `BuildLimit`, `BuildCartesianProduct` for performance/pushdown.
- See `BACKEND_GUIDE.md` for detailed guidance and the test backend for examples.

## Pattern parts

- Every comma-separated pattern part in MATCH is executed. Independent parts form Cartesian products; shared variables retain their bindings and constrain later parts. OPTIONAL MATCH fallback applies to the complete set of parts and its WHERE predicate.

## References

- Backend example: `Src/Cyphoid.Tests/TestBackend/TestOperatorFactory.cs`
- Core tests: `Src/Cyphoid.Tests/ExecutionTests.cs`, `Src/Cyphoid.Tests/GroupAndAggregateTests.cs`, `Src/Cyphoid.Tests/RelationshipTests.cs`

