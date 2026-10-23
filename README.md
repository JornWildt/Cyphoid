# C# Cyphoid parser for Cypher query language

This project implements a Cypher parser that can be used in C# to query any sort of graph backend.

Cyphoid expects developers to implement various operators that interact with their own backend.

See the [changelog](https://github.com/JornWildt/Cyphoid/blob/master/CHANGES.md) for version history and backend API changes.


## Features

- Implements a subset of Cypher.
- Fast parser implementation with Antlr.
- In-memory implementation of query plans for any backend.
- Multiple comma-separated MATCH patterns, including independent patterns and patterns sharing node variables.

## Example usage

### EXISTS subqueries

Use `WHERE EXISTS { MATCH (n)-[:RELATED_TO]->(other) }` to filter by whether a
correlated match exists, or `NOT EXISTS` to select rows without matches. EXISTS
also works in projections, supports nesting, and stops at the first result.
See [EXISTS.md](EXISTS.md) for scope, supported clauses, and examples.

### Parameters

Supply execution values through `IQueryContext.Parameters`, preferably using
immutable `QueryParameters`. Queries can use `$name` in expressions, pattern
property values, procedure arguments, and LIMIT. See [PARAMETERS.md](PARAMETERS.md)
for host setup, map-based procedure configuration, and execution-plan reuse.

### Pagination with SKIP and LIMIT

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

### Combining queries

Use `UNION` to combine independent query results and remove duplicate rows, or `UNION ALL` to retain duplicates:

```cypher
MATCH (n:pressure) RETURN n
UNION
MATCH (n:impact) RETURN n
```

Branches must return the same column names in the same order and have independent variable scopes. ORDER BY and LIMIT apply to their individual branch. `UNION DISTINCT` is accepted as an alias for UNION and is pretty-printed as UNION. Mixed UNION / UNION ALL chains and post-union subquery processing are not supported.


### Maps

Runtime maps support nested expressions, dot and dynamic key access, and node/map
projections such as `n {.name, .age}`. Procedures can declare Map parameters for
configuration: `CALL analytics.pageRank({iterations: 20}) YIELD node, score ...`.
See [MAPS.md](MAPS.md) for the host API, examples, and supported subset.

### CASE and conditional aggregation

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

### Scalar functions

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

### Node labels

Use label predicates to check a node's single type: `WHERE n:pressure OR n:activity OR n:impact`. The comparison is ordinal and case-sensitive. A null node produces null; non-node values raise a query error. Single-label predicates can also be projected, such as `RETURN n:pressure AS isPressure`.

`labels(node)` returns the node's single `IGraphNode.Type` as a string. It is an alias for Cyphoid's `type(node)`; both return null for null or non-node inputs. This differs from standard Cypher, where `labels(node)` returns a list and `type()` reads a relationship type. Cyphoid does not bind relationship values to runtime rows.

```cypher
MATCH (s:person) RETURN labels(s) AS label
```



```
// This is your implementation of the backend
IOperatorFactory factory = new OperatorFactory(Graph);

// Query input
var input = "MATCH (n:city) RETURN n.name LIMIT 1";

// Parse query
ICypherParser parser = new CypherAstParser();
var queryNode = parser.ParseQuery(input);

// Print parsed query
var prettyPrint = queryNode.PrettyPrint();

// Build logical query plan
var plan = queryNode.BuildQueryPlan();

// Build executable query plan (using your backend)
var execution = plan.BuildExecutionPlan(factory);

// Prepare query context
var context = new QueryContext(queryNode.RowSize);

// Execute actual query
var result = await execution.ExecuteAsync(context).ToListAsync();
```


## Architecture

```
Source Code
   ↓
ANTLR Lexer/Parser
   ↓
AST Builder (Visitor)
   ↓
Abstract Syntax Tree (AST)
   ↓
Cypher logical plan
   ↓
Cypher executable query plan
   ↓
<Your implementation>
```

## Backend implementation

Guidance for third-party backends is in `BACKEND_GUIDE.md`.

Rows share immutable `RowSchema` instances; `IRow.Columns` exposes a read-only
view without allocating column arrays per row. The backend guide describes row
creation and schema handling for backends and procedures.

Query exceptions, diagnostic codes, source locations, and cleanup failures are
documented in [ERROR_HANDLING.md](ERROR_HANDLING.md).

### Procedure plugins

Install third-party procedures with `ICypherParser.InstallPlugin(plugin)`, then
invoke them using `CALL namespace.procedure(...) YIELD field AS alias RETURN alias`.
Procedures consume and emit host rows, preserving metadata through the row factory
and `IRow.MergeWith`. See [PROCEDURES.md](PROCEDURES.md) for contracts, examples,
validation rules, and supported CALL/YIELD syntax.


# Developer instructions

For developing the Cyphoid library.

## Building a release

1. Update version number and more in Cyphoid.Core.csproj.
1. Build in release mode to generate NuGet package.
1. Upload package Cyphoid.N.N.N.nupkg from Src\Cyphoid.Core\bin\Release\ folder.

## ANTLR
The compiler is built using Antlr. See https://github.com/antlr/antlr4/blob/master/doc/getting-started.md

Antlr is made with Java. There are some Windows tools that make setup easier. See https://github.com/antlr/antlr4-tools

## Run ANTLR Compiler

cd into "Cyphoid.Core/Antlr".

java org.antlr.v4.Tool -Dlanguage=CSharp -visitor -o Generated Cypher.g4


## Manual Antlr installation

Install Java (version 1.7 or higher) - NOT from java.com but rather one of the newer SDKs (for instance Eclipse Temurin).

Download antlr-4.13.2-complete.jar (or whatever version) from https://www.antlr.org/download.html Save to your directory for 3rd party Java libraries, say C:\Javalib

Add antlr-4.13.2-complete.jar to CLASSPATH, either:
Permanently: Using System Properties dialog > Environment variables > Create or append to CLASSPATH variable
Temporarily, at command line.

> set CLASSPATH=D:\Javalib\antlr-4.13.2-complete.jar



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
