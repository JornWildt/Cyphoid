# Cyphoid

Cyphoid is a C# parser and query engine for a read-only subset of Cypher. It lets
you query your application's graph data using a backend you provide. Cyphoid
parses queries, builds execution plans, and supplies row-stream operators for
filtering, projection, aggregation, and sorting; your backend retrieves nodes
and relationships. The library targets .NET 10.

## A brief introduction to Cypher

Cypher describes graph patterns using nodes in parentheses and relationships
between them:

```cypher
MATCH (person:Person)-[:WORKS_FOR]->(company:Company)
WHERE company.name = $companyName
RETURN person.name AS name
ORDER BY name
LIMIT 20
```

This finds people connected to a company by a WORKS_FOR relationship, filters by
the company's name, and returns up to 20 names in order. `Person` and `Company`
are node labels; `person` and `company` are query variables. `$companyName` is a
value supplied by the host, separately from the query text.

See [Cypher support](CYPHER_SUPPORT.md) for supported clauses, functions,
aggregation, CASE, subqueries, pagination, null semantics, and compatibility limits.

## Set up a backend

Install the NuGet package in your host project:

```shell
dotnet add package Cyphoid
```

Your host supplies three pieces:

1. An `IOperatorFactory`, usually derived from `ReferenceOperatorFactory`, with
   node scan and relationship expansion operators backed by your repository.
2. An `IQueryRowFactory` that creates rows with the supplied shared `RowSchema`.
   Use the reference `Row` class or a custom implementation carrying host metadata.
3. An `IQueryContext` exposing that row factory and any query parameters or
   host-specific resources your operators need.

The [backend guide](BACKEND_GUIDE.md) describes these contracts and links to the
in-memory test backend. The [database backend guide](DATABASE_BACKEND.md) explains
database access, constraint translation, and resource handling.

## Parse and run a query

With your factory and context configured, the hosting code is:

```csharp
using Cyphoid.Core;
using Cyphoid.Core.Execution;

static async Task RunQueryAsync(
    string query,
    IOperatorFactory factory,
    IQueryContext context,
    CancellationToken ct)
{
    ICypherParser parser = new CypherAstParser();
    var queryNode = parser.ParseQuery(query);
    var plan = queryNode.BuildQueryPlan();
    var execution = plan.BuildExecutionPlan(factory);

    await foreach (var row in execution.ExecuteAsync(context, ct))
    {
        for (var i = 0; i < row.Columns.Count; i++)
        {
            Console.WriteLine($"{row.Columns[i].Name}: {row.Values[i]?.AsObject()}");
        }
    }
}
```

For the example query, expose a `companyName` value through
`IQueryContext.Parameters`, using `MixedValue.String("Acme")`. The
[parameter guide](PARAMETERS.md) includes a complete context example and explains
parameter validation and execution-plan reuse.

Results are asynchronous streams. Breaking out of the loop stops consumption and
disposes the pipeline. Pass `ct` through your storage operations to support
cancellation. Sorting and aggregation can consume input before producing results.

Parse and execution failures use structured query exceptions. See
[error handling](ERROR_HANDLING.md) for diagnostic codes and source locations.
To expose application-specific procedures through CALL/YIELD, see
[procedure plugins](PROCEDURES.md).

## Further reading

- [Cypher support](CYPHER_SUPPORT.md): query syntax, semantics, and examples.
- [Backend guide](BACKEND_GUIDE.md): operator and row contracts.
- [Database backend guide](DATABASE_BACKEND.md): database-backed operators.
- [Developer guide](DEVELOPMENT.md): building, testing, releases, and ANTLR generation.
- [Changelog](CHANGES.md): release history and API changes.
