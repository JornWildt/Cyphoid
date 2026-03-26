# Implementation summary

The Cypher engine is implemented as a pipeline of row-oriented physical operators. MATCH clauses extend the current row stream by pattern matching, WHERE filters rows, and WITH/RETURN perform projection. Aggregation is handled as part of projection: if a projection contains aggregate expressions, all non-aggregate expressions become grouping keys. The engine detects aggregates during AST analysis, then evaluates grouping keys per input row, buckets rows by key, and computes aggregate states such as count, min, and max. Pattern expansion distinguishes between Expand(All) for binding new variables and Expand(Into) for checking relationships between already bound variables. Runtime rows are represented in positional form with column metadata, allowing expressions to be compiled into efficient evaluators rather than interpreted through repeated dictionary lookups.

## Supported Cypher subset

- Clauses: MATCH, WHERE, WITH, RETURN, ORDER BY, LIMIT.
- Patterns: node patterns with optional labels and property maps; relationship patterns with direction and optional type.
- Expressions: boolean logic, comparisons, IN list, property access, COUNT(*) and basic function calls.
- Aggregation: grouping is implicit when any projection contains aggregate expressions.

## Execution model

- Pipeline is `IAsyncEnumerable` of rows; operators implement `IOperator<TId>.ExecuteAsync`.
- Rows are positional; each variable binds to a fixed `SlotIndex` in `IRow<TId>.Values`.
- Expressions compile into evaluators, not runtime dictionary lookups.

## Backend contract

- Backends implement `IOperatorFactory<TId>`; the recommended path is to inherit `ReferenceOperatorFactory<TId>`.
- Required overrides: `NewRow`, `BuildNodeScan`, `BuildExpandAll`.
- Optional overrides: `BuildExpandInto`, `BuildFilter`, `BuildProjection`, `BuildAggregationProjection`, `BuildOrderBy`, `BuildLimit`, `BuildCartesianProduct` for performance/pushdown.
- See `BACKEND_GUIDE.md` for detailed guidance and the test backend for examples.

## Known limitations

- Pattern matching currently only uses the first pattern part in a MATCH clause.
- Bidirectional relationship patterns are not supported.

## References

- Backend example: `Src/Cyphoid.Tests/TestBackend/TestOperatorFactory.cs`
- Core tests: `Src/Cyphoid.Tests/ExecutionTests.cs`, `Src/Cyphoid.Tests/GroupAndAggregateTests.cs`, `Src/Cyphoid.Tests/RelationshipTests.cs`

