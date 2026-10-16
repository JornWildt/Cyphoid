# Procedure plugins

Install application-owned plugins on an `ICypherParser` before parsing queries:

```csharp
ICypherParser parser = new CypherAstParser();
parser.InstallPlugin(new ExamplePlugin());

var query = parser.ParseQuery(
  "CALL example.numbers(10) YIELD value AS number RETURN number ORDER BY number");

// Build and execute the query with your normal operator factory and context.
var execution = query.BuildQueryPlan().BuildExecutionPlan(operatorFactory);
await foreach (var row in execution.ExecuteAsync(context))
{
  // Consume host rows as usual.
}
```

The contracts are in `Cyphoid.Core.Procedures`. `ICypherProcedurePlugin` discovers
procedures within a namespace; `ICypherProcedure` describes and executes one
procedure. No assembly scanning or global registration is performed. A third-party
NuGet package can expose a plugin class that the host constructs and installs.

## Minimal implementation

```csharp
using System.Runtime.CompilerServices;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;
using Cyphoid.Core.Procedures;

public sealed class ExamplePlugin : ICypherProcedurePlugin
{
  public string Namespace => "example";

  public IEnumerable<ICypherProcedure> GetProcedures()
    => new ICypherProcedure[] { new NumbersProcedure() };
}

public sealed class NumbersProcedure : ICypherProcedure
{
  public string Name => "numbers";
  public IReadOnlyList<ProcedureField> Parameters { get; } =
    new ProcedureField[] { new("start", MixedValue.ValueType.Int) };
  public IReadOnlyList<ProcedureField> Outputs { get; } =
    new ProcedureField[] { new("value", MixedValue.ValueType.Int) };

  public async IAsyncEnumerable<IRow> ExecuteAsync(
    IQueryContext context,
    IRow inputRow,
    IReadOnlyList<MixedValue> arguments,
    IReadOnlyList<IRowColumn> outputColumns,
    [EnumeratorCancellation] CancellationToken ct)
  {
    // Replace this with asynchronous repository access when appropriate.
    await Task.Yield();
    var start = arguments[0].AsInt();
    for (var i = 0; i < 3; i++)
    {
      ct.ThrowIfCancellationRequested();
      var output = context.RowFactory.EmptyRow(inputRow, outputColumns);
      output.Values[0] = MixedValue.Int(start + i);
      yield return output;
    }
  }
}
```

Inject repositories or other application services through plugin/procedure
constructors. Execution also receives the current `IQueryContext` for host-specific
query resources. Avoid capturing a transaction while installing or parsing.

## Names and registration

- Names are ordinal and case-sensitive. Cypher clause keywords remain case-insensitive.
- Namespaces can contain several identifier segments, such as `company.knowledge`.
  Procedure names are a single identifier. Use non-keyword ASCII identifiers:
  a letter followed by letters, digits, or underscores, as supported by the grammar.
- Duplicate namespaces, duplicate procedure names, duplicate field names, invalid
  field types, and procedures without outputs are rejected during installation.
- Discovery occurs once per installation attempt. Signatures are copied into
  read-only snapshots. A failed installation publishes no partial registrations.
- Each parse uses a catalog snapshot. Later installs
  affect later parses; existing queries retain their procedure registrations.
- The host owns plugin lifetime. Procedure instances must support concurrent and
  repeated calls, using local invocation state. Cyphoid does not dispose plugins.

## CALL and YIELD

```cypher
MATCH (n:Concept)
CALL knowledge.neighbors(n, 'RELATED_TO')
YIELD node AS related, score
WHERE score > 0.5
RETURN n.name, related.name, score
ORDER BY score DESC
LIMIT 10
```

This example assumes an installed `knowledge.neighbors` procedure declaring
`Node, String` arguments and `node: Node, score: Double` outputs.

A leading CALL executes once using the engine's single empty input row. After
MATCH or WITH, it executes once per incoming row. Each procedure result produces
one combined row; no procedure results means no downstream row for that input.
Cyphoid does not cache, deduplicate, or parallelize invocations.

YIELD selects output fields and optionally aliases them. Existing variables stay
in scope; aliases must not collide with them. WHERE after YIELD sees both input
and yielded variables. Yielded nodes can participate in subsequent MATCH clauses.
Further WITH, RETURN, aggregation, sorting, limiting, and UNION work normally.

This initial subset requires parentheses, explicit YIELD fields, and a final
RETURN. Standalone calls, YIELD *, optional arguments, overloads, void procedures,
and CALL subqueries are not supported. Aggregate arguments must first be
projected in WITH.

## Values, schemas, and errors

Arguments and results use existing MixedValue types only. There are no list, map,
or path values added by this API. For collections, return multiple rows; for paths,
for example, return path ID, step index, and node columns.

Signatures are fixed and ordered. Argument counts, names, variable bindings, and
YIELD fields are checked while parsing. Runtime types must match exactly, with no
implicit conversions. A null argument/output is accepted only when its field is
nullable or explicitly typed Null. Null arguments are passed to the procedure
when allowed, rather than automatically skipping invocation.

Every result must contain the entire declared output schema, using the supplied
local indexes starting at zero, even if YIELD selects only some fields. Cyphoid
validates column names, indexes, types, width, values, and nullability. Procedure
failures are reported with the qualified procedure name and original inner
exception. Cancellation exceptions propagate unchanged.

`RuntimeException` is public. Invalid result schemas use the `InvalidProcedureResult`
code; plugin invocation/enumeration failures use `ProcedureFailed`. Cleanup failures
do not replace an existing procedure error or cancellation. See
[ERROR_HANDLING.md](ERROR_HANDLING.md) for structured diagnostics and access to
secondary cleanup exceptions.

## Host metadata

The input row is borrowed and must not be mutated. Create each output through
`context.RowFactory.EmptyRow(inputRow, outputColumns)` so the host can copy input
metadata. A host-specific procedure can enrich that metadata on its output row.
Do not modify an output after yielding it.

Cyphoid creates a row from the procedure result through
`RowFactory.EmptyRow(result, queryColumns)`, maps selected values to their final
query slots, then calls `inputRow.MergeWith(mappedResult)`. Input slots on the
mapped result are left unset. This matches MergeWith's shared-slot semantics;
it does not assume MergeWith concatenates columns.

The host controls metadata copying and merging, including deduplication when
metadata appears on both rows. Metadata is never interpreted as MixedValue data.

## Enumeration and cancellation

Cyphoid consumes procedure results through `IAsyncEnumerable<IRow>`. Procedures
can produce rows gradually, although some algorithms must complete their
calculation before producing the first row.

A query may stop consuming results early, for example because of `LIMIT`.
Cyphoid disposes the result enumerator on completion, failure, or early exit,
allowing the procedure's `finally` blocks to release resources. It does not
dispose the procedure or plugin object.

Put resource cleanup in `finally` or `using` blocks. Do not assume that every
result row will be requested, or that ordinary code after the last `yield return`
will execute.

Pass a cancellation token to `execution.ExecuteAsync(context, ct)` or enumerate
`execution.ExecuteAsync(context).WithCancellation(ct)`. All reference operators
forward cancellation through the pipeline to backend operators and procedures.
If both forms supply different tokens, cancellation of either stops execution.

Procedure implementations must observe their `ct` parameter: pass it to
asynchronous repository calls and check `ct.ThrowIfCancellationRequested()`
periodically during CPU work. Cancellation is cooperative; Cyphoid cannot
interrupt plugin or backend code that ignores the token. Result enumerators are
disposed when cancellation unwinds the pipeline, and cancellation exceptions
propagate unchanged.
