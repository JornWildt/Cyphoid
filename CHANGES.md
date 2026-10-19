# Changelog

Changes are listed by version, newest first. Historical dates follow version updates in Git, rather than verified package publication dates. Version 0.10.0 appears in package release notes but has no separate version commit.

## Unreleased

- Fixed null propagation for scalar equality/inequality and range comparisons, and three-valued AND/OR/NOT in synchronous and asynchronous EXISTS expressions. IN now uses null-propagating scalar equality. WHERE retains only true; use IS NULL instead of = null. Grouping, DISTINCT, and ordering keep their structural semantics. Host scan/expansion constraints retain unknown candidates for residual evaluation; nullable SQL range translations need the same candidate preservation (see BACKEND_GUIDE.md).

- Renamed relationship-type arguments from `relationLabel` to `relationshipType`, the node-scan hint from `targetRelationshipLabel` to `targetRelationshipType`, and corresponding operator members. Node label arguments are unchanged. Update named arguments and references to `ExpandIntoReferenceOperator.RelationLabel` (now `RelationshipType`).

## 1.0.0 ? 2026-10-18

- Added `<`, `<=`, `>`, and `>=` MATCH predicate pushdown with explicit PropertyComparison values, reversed-operand normalization, and residual preservation of range type errors.

- Added conservative MATCH-local WHERE equality pushdown into node scan and expansion property constraints, including independent pattern parts and OPTIONAL MATCH branches. Constraints carry explicit query/pattern equality semantics and execution-bound values; residual predicates preserve runtime behavior. Backend property filtering must honor PropertyFilterCondition.IsMatch.

- Added correlated EXISTS subqueries in expressions, including NOT EXISTS, nested queries, WITH scope preservation, procedure calls, optional RETURN, pattern shorthand, and UNION. Evaluation is asynchronous, short-circuits at the first row, preserves outer metadata, and propagates parameters/cancellation. ORDER BY now supports boolean results.
- Added `$name` query parameters supplied through `IQueryContext.Parameters`, immutable QueryParameters snapshots, required-name discovery, and located MissingParameter errors before backend binding. Parameters work in expressions, map-based procedure arguments, node property filters, and WITH/RETURN LIMIT.
- Execution plans now compile reusable binding templates with execution-local parameter values and aggregation state, retaining shared schemas. Backend factory hooks run during execution binding; custom logical plans implement Compile to return a parameter-binding factory. See PARAMETERS.md.
- Rows now share immutable `RowSchema` instances instead of allocating column arrays per row. Schemas snapshot column definitions once; projection, aggregation, scans, expansions, and procedures reuse them. UNION normalizes output schemas while preserving host metadata.
- Breaking schema API change: `IRow.Schema` replaces mutable column-array storage, with `IRow.Columns` exposing a read-only view. Row factories, operator factories, copies, and procedure output-schema arguments now use `RowSchema`. See BACKEND_GUIDE.md for migration steps.
- Added immutable Map values to MixedValue, nested map literals, dot/bracket lookup, node/map projections, structural grouping and hashing, and map parameters/results for procedures. Host maps use `Map`, `AsMap`, and `TryGetMap`; object conversion supports nested string-keyed dictionaries. Missing/null lookups return null; invalid lookup types raise query errors.
- Added public runtime errors with stable diagnostic codes, error phases, and source spans. Syntax errors retain individually accessible diagnostics. Scalar conversions retain inner exceptions and compiled query expressions report their failing location.
- Built-in function signatures and unsupported aggregate placement are now validated during parsing. `type`/`labels` and aggregates reject missing or extra arguments; only `count(*)` accepts `*`. Oversized LIMIT values are rejected instead of truncated.
- Integer arithmetic now reports overflow and division/remainder by zero as runtime query errors. Double arithmetic retains .NET IEEE semantics.
- Procedure cleanup failures no longer replace an existing enumeration, result-validation, host row-composition, or cancellation failure. Secondary failures are available through `CypherExceptionDiagnostics.GetCleanupExceptions`.
- Added end-to-end query cancellation through reference operators, backend operators, and procedure invocations, supporting both `ExecuteAsync(context, ct)` and `.WithCancellation(ct)`. Cancellation propagates unchanged and disposes active enumerators.
- Breaking backend API change: `IOperator.ExecuteAsync` now takes `CancellationToken ct = default`. Implementations must accept and forward the token; async iterator implementations should mark it with `[EnumeratorCancellation]`. Existing calls without a token remain valid.

- Added `UNION` / `UNION DISTINCT` and `UNION ALL` with independent branch scopes and validation of output column names and order. UNION removes duplicate complete rows, including matching node identities and nulls; UNION ALL preserves duplicates. Mixing distinct and ALL forms in a single chain is rejected.

## 0.15.0 — 2026-10-15

- Added `n:label` predicates for checking a node's single type in WHERE and projected expressions. Predicates support boolean combinations, use ordinal case-sensitive comparison, and return null for null nodes.

- Added `coalesce`, `toLower`, `toUpper`, `trim`, `ltrim`, `rtrim`, `toString`, `toInteger`, `toFloat`, `toBoolean`, string `size`, `substring`, `replace`, `left`, `right`, `abs`, `round`, `floor`, and `ceil` using .NET semantics. Includes lazy coalescing, null propagation, argument validation, invariant casing/conversion, UTF-16 string indexing, and .NET midpoint rounding modes.

## 0.14.0 — 2026-10-15

- Two-node patterns with an unbound left node and an already-bound right node now expand from the bound endpoint instead of scanning the left node. Traversal direction is reversed internally while retaining labels, property constraints, and OPTIONAL MATCH fallback.
- MATCH now executes all comma-separated pattern parts, combining independent parts and enforcing shared-variable constraints. OPTIONAL MATCH fallback applies only when the complete set of parts has no matches.
- Added `WITH ... WHERE` to filter projected aliases and aggregate results. WHERE uses the projected scope and follows any WITH ORDER BY and LIMIT modifiers.

## 0.13.0 — 2026-10-14

- Removed obsolete optional flags from expansion plans and operators. Expansion always checks relationships and emits only matches; clause-level OPTIONAL MATCH supplies null fallback rows.
- Removed per-expansion optional fallback and match tracking from the test backend.
- Breaking backend API change: removed the `bool optional` parameter from `IOperatorFactory.BuildExpandAll`, `IOperatorFactory.BuildExpandInto`, and the corresponding `ReferenceOperatorFactory` methods. Backend overrides and calls must remove this parameter and leave optional fallback to the enclosing clause operator.
- Removed the optional parameter from `PatternNode.BuildPlan` and the expansion plan and reference operator constructors.

## 0.12.0 — 2026-10-14

### Added

- `rand()` scalar function returning a random double in the range [0, 1).
- `id(node)` scalar function returning a backend-defined identifier. `id(null)` returns null.
- `labels(node)` as a string-returning alias for Cyphoid's `type(node)`. Unlike standard Cypher, it returns the node's single type rather than a list.
- `IS NULL` and `IS NOT NULL` predicates.
- Case-sensitive, literal `CONTAINS`, `STARTS WITH`, and `ENDS WITH` string operators. Null or non-string operands return null.
- `ORDER BY` and `LIMIT` on `WITH` clauses.
- Support for backend-specific row metadata, preserving and merging it through row operations. Aggregated results do not collect source-row metadata.

### Fixed

- `ORDER BY` can reference input variables omitted from an ordinary `RETURN`, such as `RETURN c.name ORDER BY c.name`. Internal sorting variables are removed from the output. DISTINCT and aggregate projections retain their projected scope.
- Recognition of the two-token `STARTS WITH` and `ENDS WITH` operators in AST construction.
- `OPTIONAL MATCH` now matches the complete clause, including its WHERE predicate, for each incoming row. When no complete match exists, it emits one row with newly introduced variables null and preserves existing bindings. Unrelated candidates and partial matches no longer leak into the result.
- Incoming expansion in the test backend uses the complete output column layout.

### Backend API changes

- Removed generic ID parameters from execution interfaces, plans, and operators. `GraphNode<TId>` and `GraphEdge<TId>` retain their generic identifiers in the reference backend.
- Added `MixedValue GetId()` to the non-generic `IGraphNode` interface. Custom node implementations must provide it. `GraphNode<TId>` uses `MixedValue.FromObject(Id)` and permits overriding the conversion for custom identifier types.
- Added a clause-level optional-match plan and reference operator. Scan-based optional patterns execute against each incoming row.

## 0.11.0 — 2026-10-13

- Pass the immediate outgoing relationship type to node scans, allowing backends to exclude nodes without a relationship of that type.
- Extended the node-scan factory contract with a target relationship label.

## 0.10.0 — Historical package release notes

- Recorded a change to optional matching in the ExpandInto reference operator. The complete clause-level correction is included in 0.12.0.

## 0.9.0 — 2026-09-16

- Added initial `OPTIONAL MATCH` support.
- Fixed the output column count used by ExpandAll.
- Updated the target framework from .NET 8 to .NET 10.
- Updated runtime and test dependencies.

## 0.8.0 — 2026-04-08

- Added undirected relationship patterns, including `-[]-`.
- Fixed a null-pattern handling problem.

## 0.7.2 — 2026-04-07

- Allowed chained comparison expressions such as `a = b = c`.
- Added the node `type()` scalar function after the preceding version update.

## 0.7.1 — 2026-04-06

- Recorded the unary-operator fixes in an updated package version and release notes.

## 0.7.0 — 2026-04-06

- Added `SUM`, `MIN`, `MAX`, `AVG`, and `COUNT(expression)` aggregation support; aggregate functions skip null values.
- Added `DISTINCT` projection support.
- Added arithmetic operators `+`, `-`, `*`, `/`, and `%`, and ordered comparisons `<`, `>`, `<=`, and `>=`.
- Improved decimal-number handling and fixed unary operators.
- Introduced dedicated parse and runtime exceptions and extended the function-definition framework.
- Converted pipeline operator definitions to records.

## 0.6.0 — 2026-03-26

- Added `WITH` projections, implicit grouping, and `COUNT(*)` aggregation.
- Implemented runtime `ORDER BY`.
- Unified operator and projection output as positional rows containing `MixedValue` values and column metadata.
- Added backend implementation and architecture documentation.

## 0.5.0 — 2026-03-22

- Fixed relationship-direction checks in ExpandInto.
- Made edge target identifiers generic, matching node identifier types.

## 0.4.0 — 2026-03-22

- Added multiple MATCH clauses and bound/unbound variable tracking.
- Added ExpandInto for checking relationships between already-bound nodes.
- Introduced generic graph-node identifiers and node types.
- Improved query pretty-printing.

## 0.3.0 — 2026-03-19

- Added graph nodes as expression values.
- Improved null handling.

## 0.2.0 — 2026-03-19

- Added a reference backend implementation and async sequence support.
- Added NuGet packaging with README and symbol packages.

## 0.1.0 — 2026-03-19

- Initial Cypher parser, AST, pretty-printer, and logical planning implementation.
- Added node scans and relationship expansion with label and property constraints.
- Added WHERE filtering, RETURN projections, property access, and the `IN` operator.
- Introduced `MixedValue` and backend operator contracts.
