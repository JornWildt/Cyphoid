# C# Cyphoid parser for Cypher query language

This project implements a Cypher parser that can be used in C# to query any sort of graph backend.

Cyphoid expects developers to implement various operators that interact with their own backend.

See the [changelog](https://github.com/JornWildt/Cyphoid/blob/master/CHANGES.md) for version history and backend API changes.


## Features

- Implements a subset of Cyhper.
- Fast parser implementation with Antlr.
- In-memory implementation of query plans for any backend.
- Multiple comma-separated MATCH patterns, including independent patterns and patterns sharing node variables.

## Example usage

### Scalar functions

Supported scalar functions include `coalesce`, `toLower`, `toUpper`, `trim`, `ltrim`, `rtrim`, `toString`, `toInteger`, `toFloat`, `toBoolean`, `size`, `substring`, `replace`, `left`, `right`, `abs`, `round`, `floor`, and `ceil`. Names are case-insensitive.

These functions use .NET semantics rather than full Cypher compatibility:

- `coalesce(value, ...)` evaluates arguments in order and returns the first non-null value, without evaluating later arguments. It requires at least one argument.
- Other functions return null when any supplied argument is null. Invalid argument counts, types, conversions, and ranges raise query errors.
- Casing uses `ToLowerInvariant` / `ToUpperInvariant`. Trim functions use .NET whitespace rules and accept one string argument.
- Conversions use `Convert.ToString`, `Convert.ToInt64`, `Convert.ToDouble`, and `Convert.ToBoolean` with invariant culture. For example, `toInteger(2.5)` returns `2`, `toString(true)` returns `"True"`, and `toBoolean(1)` returns true. Nodes cannot be converted.
- `size(string)` counts UTF-16 code units. `substring(string, start [, length])`, `left(string, length)`, and `right(string, length)` use zero-based .NET substring indexing and require valid ranges; oversized lengths raise errors. No list values are introduced.
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

When combined in one WITH clause, modifiers follow the order `WITH ... ORDER BY ... LIMIT ... WHERE ...`. WHERE sees the projected variables and filters after LIMIT. To filter before sorting or limiting, use a second WITH clause.

### Node labels

`labels(node)` returns the node's single `IGraphNode.Type` as a string. It is an alias for Cyphoid's `type(node)`; both return null for null or non-node inputs. This differs from standard Cypher, where `labels(node)` returns a list and `type()` reads a relationship type. Cyphoid does not currently bind relationship values to runtime rows.

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

