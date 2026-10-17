# Error handling

Query errors use public exceptions in `Cyphoid.Core.Exceptions`:

- `ParseException`: syntax and semantic errors detected while parsing, including
  unknown variables/functions/procedures, invalid function signatures, invalid
  YIELD fields, and unsupported aggregate placement.
- `RuntimeException`: invalid values or procedure results encountered while
  executing a query. Errors may arise during async enumeration, after some
  results have already been returned.
- Both derive from `CypherException`, which exposes `Code`, `Phase`, `SourceSpan`,
  and `Diagnostics`. Inspect codes instead of matching message text.

```csharp
try
{
    var query = parser.ParseQuery(queryText);
    var execution = query.BuildQueryPlan().BuildExecutionPlan(operatorFactory);
    await foreach (var row in execution.ExecuteAsync(context, ct))
    {
        // Consume results.
    }
}
catch (OperationCanceledException)
{
    // Cancellation is not translated into a query error.
    throw;
}
catch (CypherException error)
{
    foreach (var diagnostic in error.Diagnostics)
    {
        Console.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
        if (diagnostic.SourceSpan is { } span)
            Console.WriteLine($"Line {span.Line}, column {span.Column}");
    }

    // InnerException retains the cause when an external failure was translated.
}
```

The try block covers parsing, plan construction, and enumeration. Catch only
`ParseException` or `RuntimeException` when the application needs to distinguish
those phases. Plugin configuration mistakes remain argument exceptions rather
than query errors. Arbitrary backend, row-factory, and engine programming failures
are not blanket-wrapped as invalid queries.

## Codes and source locations

Missing execution parameters use `MissingParameter`, with the location of the
first reference. Parsed queries validate all required names before invoking backend
factory hooks, including references in UNION branches or queries with no results.

`CypherErrorCode` includes syntax, name-resolution, signature, type, argument-value,
arithmetic, and procedure errors. `InvalidQuery` is the fallback for other semantic
query errors. A `CypherDiagnostic` contains a code, message, phase, and optional
source span.

Syntax errors retain individual diagnostics in source order. Semantic and runtime
errors normally contain one diagnostic. Compiled expression failures retain the
innermost available location; procedure invocation failures point to CALL.

`SourceSpan.Start` and `Length` use zero-based UTF-16 offsets into the query string.
Lines are one-based and columns zero-based. EOF errors can have zero-length spans.
Locations may be absent for values evaluated directly or AST nodes constructed
by application code rather than the parser.

Built-in function argument counts are checked during parsing, including aggregate
functions and `type`/`labels`. Only `count(*)` accepts `*`. Unsupported combinations
such as `coalesce(count(*), 0)` fail during parsing; project the aggregate in WITH
and use the resulting variable instead. Function evaluators retain defensive
signature checks for application-constructed ASTs.

## Numeric and scalar failures

Integer division/remainder by zero raises `RuntimeException` with code
`DivisionByZero`. Integer addition, subtraction, multiplication, negation, and
division check overflow and report `NumericOverflow`, preserving the original
arithmetic exception. Aggregate integer arithmetic uses the same rules.

Double arithmetic retains .NET IEEE behavior, including infinity and NaN; it is
not changed into integer-style division errors. Scalar conversions and range
checks report `InvalidArgumentValue`, `TypeMismatch`, or `NumericOverflow` and
preserve their original exceptions. Scalar errors include the function name.

## Procedure failures and cleanup

`ProcedureFailed` reports exceptions from invocation, result enumeration, or
cleanup, retaining the qualified procedure name and original inner exception.
`InvalidProcedureResult` identifies a declared-schema violation separately from
an exception thrown by plugin execution. Runtime argument type mismatches use
`TypeMismatch` before invoking the procedure.

If procedure enumeration, result validation, or host row composition fails and
disposing the procedure enumerator also fails, the primary exception remains
the one thrown. This also preserves an existing cancellation exception.
Secondary cleanup failures can be retrieved from any primary exception:

```csharp
foreach (var cleanupError in CypherExceptionDiagnostics.GetCleanupExceptions(error))
{
    // Record the secondary failure without losing the primary error.
}
```

A cleanup failure without a primary failure is reported normally, including on
early exit. No automatic retries or rollback are performed; rows already consumed
and any plugin side effects are not undone.
