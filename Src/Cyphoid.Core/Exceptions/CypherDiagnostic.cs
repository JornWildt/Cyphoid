namespace Cyphoid.Core.Exceptions;

public enum CypherErrorCode
{
  SyntaxError, InvalidQuery, UnknownVariable, UnknownFunction, UnknownProcedure,
  InvalidArgumentCount, InvalidAggregateUsage, InvalidYield, TypeMismatch,
  InvalidArgumentValue, DivisionByZero, NumericOverflow, InvalidProcedureResult,
  ProcedureFailed, EvaluationFailed, MissingParameter
}

public enum CypherErrorPhase { Parsing, Execution }

/// <summary>Zero-based UTF-16 offset and column; one-based line; Length is in UTF-16 code units.</summary>
public sealed record SourceSpan(int Start, int Length, int Line, int Column);

public sealed record CypherDiagnostic(
  CypherErrorCode Code, string Message, CypherErrorPhase Phase, SourceSpan? SourceSpan);

/// <summary>Secondary cleanup failures never replace an existing error or cancellation.</summary>
public static class CypherExceptionDiagnostics
{
  private static readonly object CleanupKey = new();

  public static IReadOnlyList<Exception> GetCleanupExceptions(Exception exception)
  {
    ArgumentNullException.ThrowIfNull(exception);
    return exception.Data[CleanupKey] is List<Exception> errors ? errors.AsReadOnly() : Array.Empty<Exception>();
  }

  internal static void AddCleanupException(Exception primary, Exception cleanup)
  {
    if (primary.Data[CleanupKey] is not List<Exception> errors)
      primary.Data[CleanupKey] = errors = new List<Exception>();
    errors.Add(cleanup);
  }
}
