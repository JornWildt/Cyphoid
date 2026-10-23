# Cypher support

Cyphoid implements a read-only subset of Cypher. This guide describes the query
syntax, expression behavior, and compatibility limits. For hosting a query, see
[README.md](README.md).

## Patterns, filtering, and projection

MATCH binds node variables and traverses typed relationships. Node patterns accept
a single label and a property map, such as `(p:Person {name: $name})`. Relationship
patterns support outgoing, incoming, and undirected traversal. Comma-separated
patterns all participate in matching; independent parts form Cartesian products,
and shared variables constrain subsequent parts.

WHERE filters matches; RETURN selects result columns. OPTIONAL MATCH preserves an
input row with null bindings when its complete pattern and WHERE produce no match.
WITH projects values into the next scope and supports filtering, ordering, and
pagination. RETURN DISTINCT removes duplicate result rows.

Aggregation uses implicit grouping: non-aggregate projection expressions are the
grouping keys. `count(*)` counts rows, while `count(value)` ignores null values.
The aggregate functions include `count`, `sum`, `avg`, `min`, and `max`.

Cyphoid does not support write clauses, variable-length traversal, runtime list
or path values, or bare pattern predicates. Use EXISTS for pattern existence tests.

## Node labels

Use label predicates to check a node's single type: `WHERE n:pressure OR n:activity OR n:impact`. The comparison is ordinal and case-sensitive. A null node produces null; non-node values raise a query error. Single-label predicates can also be projected, such as `RETURN n:pressure AS isPressure`.

`labels(node)` returns the node's single `IGraphNode.Type` as a string. It is an alias for Cyphoid's `type(node)`; both return null for null or non-node inputs. This differs from standard Cypher, where `labels(node)` returns a list and `type()` reads a relationship type. Cyphoid does not bind relationship values to runtime rows.

```cypher
MATCH (s:person) RETURN labels(s) AS label
```

## Parameters

Supply execution values through `IQueryContext.Parameters`, preferably using
immutable `QueryParameters`. Queries can use `$name` in expressions, pattern
property values, procedure arguments, and LIMIT. See [PARAMETERS.md](PARAMETERS.md)
for host setup, map-based procedure configuration, and execution-plan reuse.

## Scalar functions

Supported scalar functions include `coalesce`, `toLower`, `toUpper`, `trim`, `ltrim`, `rtrim`, `toString`, `toInteger`, `toFloat`, `toBoolean`, `size`, `substring`, `replace`, `left`, `right`, `abs`, `round`, `floor`, and `ceil`. Names are case-insensitive.

These functions use .NET semantics rather than full Cypher compatibility:

- `coalesce(value, ...)` evaluates arguments in order and returns the first non-null value, without evaluating later arguments. It requires at least one argument.
- Other functions return null when any supplied argument is null. Invalid argument counts, types, conversions, and ranges raise query errors.
- Casing uses `ToLowerInvariant` / `ToUpperInvariant`. Trim functions use .NET whitespace rules and accept one string argument.
- Conversions use `Convert.ToString`, `Convert.ToInt64`, `Convert.ToDouble`, and `Convert.ToBoolean` with invariant culture. For example, `toInteger(2.5)` returns `2`, `toString(true)` returns `"True"`, and `toBoolean(1)` returns true. Nodes cannot be converted.
- `size(string)` counts UTF-16 code units. `substring(string, start [, length])`, `left(string, length)`, and `right(string, length)` use zero-based .NET substring indexing and require valid ranges; oversized lengths raise errors. List values are not supported.
- `replace(string, search, replacement)` performs ordinal, case-sensitive replacement; the search string must not be empty.
- `abs(number)` preserves integer or double type. `floor(number)`, `ceil(number)`, and `round(number [, precision [, mode]])` return doubles. Precision is an integer from 0 to 15. Rounding defaults to `ToEven`; mode accepts .NET `MidpointRounding` names: `ToEven`, `AwayFromZero`, `ToZero`, `ToNegativeInfinity`, and `ToPositiveInfinity`.

Scalar functions can wrap other scalar expressions. To transform an aggregate result, project it first: `WITH avg(p.score) AS average RETURN round(average)`.


`WITH ... WHERE` filters projected values and aggregate results:

```cypher
MATCH (p:person)
WITH p.country AS country, count(*) AS total
WHERE total > 10
RETURN country, total
```

When combined in one WITH clause, modifiers follow the order `WITH ... ORDER BY ... SKIP ... LIMIT ... WHERE ...`. WHERE sees the projected variables and filters after LIMIT. To filter before sorting or limiting, use a second WITH clause.

## CASE and conditional aggregation

CASE selects the first matching branch and evaluates only its result:

```cypher
RETURN CASE WHEN $score >= 80 THEN 'high' ELSE 'low' END AS category
```

Simple CASE evaluates a selector once and compares it with each WHEN value:

```cypher
RETURN CASE $status WHEN 'open' THEN 1 WHEN 'closed' THEN 0 ELSE null END AS state
```

A searched WHEN condition must be boolean or null; only true matches. Simple CASE
uses query equality, so `CASE null WHEN null ...` does not match. Omitted ELSE
returns null. CASE supports nesting, parameters, maps, and EXISTS expressions.
Unselected conditions/results are not evaluated. Aggregate calls inside CASE
must be projected in WITH first; CASE can be an argument to an aggregate.

For project/activity totals, OPTIONAL MATCH preserves projects with no activities,
and `count(a)` counts only non-null matches:

```cypher
MATCH (p:project)
OPTIONAL MATCH (a:activity)-[:in_project]->(p)
WITH p, count(a) AS activity_count
RETURN count(CASE WHEN activity_count > 0 THEN 1 END) AS projects_with_activities,
       count(CASE WHEN activity_count = 0 THEN 1 END) AS projects_without_activities
```

`sum(CASE WHEN ... THEN 1 ELSE 0 END)` also supports conditional totals.
To test existence without counting every activity, use:

```cypher
MATCH (p:project)
RETURN count(CASE WHEN EXISTS {
         MATCH (p)<-[:in_project]-(:activity)
       } THEN 1 END) AS projects_with_activities,
       count(CASE WHEN NOT EXISTS {
         MATCH (p)<-[:in_project]-(:activity)
       } THEN 1 END) AS projects_without_activities
```

Bare pattern expressions such as `WHEN (p)<-[:in_project]-(:activity)` are not
supported; use EXISTS with MATCH. Multiple matching activities count individually;
if the graph contains duplicate activity relationships, `count(a)` reflects the
matching rows rather than a distinct activity count.

## Maps

Runtime maps support nested expressions, dot and dynamic key access, and node/map
projections such as `n {.name, .age}`. Procedures can declare Map parameters for
configuration: `CALL analytics.pageRank({iterations: 20}) YIELD node, score ...`.
See [MAPS.md](MAPS.md) for the host API, examples, and supported subset.

## EXISTS subqueries

Use `WHERE EXISTS { MATCH (n)-[:RELATED_TO]->(other) }` to filter by whether a
correlated match exists, or `NOT EXISTS` to select rows without matches. EXISTS
also works in projections, supports nesting, and stops at the first result.
See [EXISTS.md](EXISTS.md) for scope, supported clauses, and examples.

## Pagination with SKIP and LIMIT

SKIP discards a number of rows; LIMIT bounds how many following rows are returned:

```cypher
MATCH (n:Person)
RETURN n.name, id(n) AS nodeId
ORDER BY n.name, nodeId
SKIP $offset
LIMIT $pageSize
```

WITH and RETURN accept `ORDER BY ... SKIP ... LIMIT ...` in that order. SKIP
also works without LIMIT. Its value is an integer literal or parameter between
0 and Int32.MaxValue. Null, negative, non-integer, and oversized parameter values
raise query errors. ORDER BY defines the page order; use a unique tie-breaker
for stable pages. Without ORDER BY the row order is unspecified.

SKIP applies after projection, aggregation, DISTINCT (for RETURN), and sorting,
and before LIMIT. A WITH WHERE predicate follows these modifiers. Each UNION
branch and EXISTS subquery applies its own SKIP. SKIP runs in the row pipeline;
it is not pushed into backend scans or expansions.

## Combining queries

Use `UNION` to combine independent query results and remove duplicate rows, or `UNION ALL` to retain duplicates:

```cypher
MATCH (n:pressure) RETURN n
UNION
MATCH (n:impact) RETURN n
```

Branches must return the same column names in the same order and have independent variable scopes. ORDER BY and LIMIT apply to their individual branch. `UNION DISTINCT` is accepted as an alias for UNION and is pretty-printed as UNION. Mixed UNION / UNION ALL chains and post-union subquery processing are not supported.

## Procedure calls

Install procedures with `ICypherParser.InstallPlugin(plugin)` before parsing.
Use `CALL namespace.procedure(...) YIELD field AS alias` to invoke a procedure
and select output fields. See [PROCEDURES.md](PROCEDURES.md) for the supported
syntax, signatures, schema validation, and host metadata handling.

## Null semantics

Comparisons (`=`, `<>`, `<`, `<=`, `>`, `>=`) return null if either operand is
null, even for `null = null`. Use `IS NULL` / `IS NOT NULL` to test nulls.
AND, OR, and NOT use three-valued logic: false AND null is false, true OR null
is true, and NOT null is null. Only a definite false left operand short-circuits
AND; only a definite true left operand short-circuits OR. WHERE keeps only true.
IN returns true for a definite match, null when no match is found but a comparison
is unknown, and false otherwise (including an empty list).
Grouping and DISTINCT use structural equality, where matching null values form
one group. ORDER BY uses sorting rules that order null values separately from
non-null values. These rules are distinct from query comparison semantics.
Backend implementers should read the null-constraint requirements in
[BACKEND_GUIDE.md](BACKEND_GUIDE.md).

These truth tables follow [Cypher's documented null semantics](https://neo4j.com/docs/cypher-manual/current/values-and-types/working-with-null/).
