using System.Runtime.CompilerServices;
using Cyphoid.Core.Exceptions;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;
using Cyphoid.Core.Procedures;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.ReferenceBackend;

internal sealed record ProcedureCallReferenceOperator(
  IOperator Input, CallNode Call, IReadOnlyList<AsyncRowEvaluator> Arguments, RowSchema OutputSchema) : IOperator
{

  public IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context, CancellationToken ct = default) => Execute(context, ct);

  private async IAsyncEnumerable<IRow> Execute(
    IQueryContext context, [EnumeratorCancellation] CancellationToken ct = default)
  {
    ct.ThrowIfCancellationRequested();
    var registration = Call.Registration;
    var columns = OutputSchema;
    await foreach (var input in Input.ExecuteAsync(context, ct).WithCancellation(ct))
    {
      ct.ThrowIfCancellationRequested();
      var arguments = new MixedValue[Arguments.Count];
      for (var i = 0; i < arguments.Length; i++) arguments[i] = await Arguments[i](input, context, ct);
      for (var i = 0; i < arguments.Length; i++)
        if (!ProcedureRegistration.Accepts(registration.Parameters[i], arguments[i]))
        {
          var error = new RuntimeException($"Procedure '{registration.QualifiedName}' argument '{registration.Parameters[i].Name}' requires {registration.Parameters[i].Type} (nullable: {registration.Parameters[i].Nullable}); got {arguments[i].Type}.");
          error.Locate(Call.Arguments[i].SourceSpan ?? Call.SourceSpan);
          throw error;
        }

      // Wrap both synchronous invocation errors and deferred enumeration errors.
      IAsyncEnumerator<IRow> results;
      Exception? primary = null;
      try
      {
        results = registration.Procedure.ExecuteAsync(context, input, Array.AsReadOnly(arguments),
          columns, ct).GetAsyncEnumerator(ct);
      }
      catch (Exception exception) when (exception is not OperationCanceledException)
      {
        throw Failure(exception);
      }
      try
      {
        while (true)
        {
          IRow result;
          try
          {
            ct.ThrowIfCancellationRequested();
            if (!await results.MoveNextAsync()) break;
            ct.ThrowIfCancellationRequested();
            result = results.Current;
          }
          catch (Exception exception)
          {
            primary = exception;
            if (exception is OperationCanceledException) throw;
            primary = Failure(exception);
            throw primary;
          }

          try { ValidateResult(result, columns); }
          catch (InvalidProcedureResultException exception)
          {
            primary = Failure(exception, CypherErrorCode.InvalidProcedureResult);
            throw primary;
          }
          catch (Exception exception)
          {
            primary = exception;
            throw;
          }

          // MergeWith uses shared query slots, not concatenation. Leave input
          // slots unset in the right row so the host merge preserves their values.
          IRow combined;
          try
          {
            var yielded = context.RowFactory.EmptyRow(result, Call.OutputColumns);
            foreach (var binding in Call.Yields)
              yielded.Values[binding.Variable.SlotIndex] = result.Values[binding.OutputIndex] ?? MixedValue.Null();
            combined = input.MergeWith(yielded);
          }
          catch (Exception exception)
          {
            primary = exception; // Preserve host failures without relabeling them as plugin errors.
            throw;
          }
          yield return combined;
        }
      }
      finally
      {
        try { await results.DisposeAsync(); }
        catch (Exception exception)
        {
          if (primary != null)
            CypherExceptionDiagnostics.AddCleanupException(primary, exception);
          else if (exception is OperationCanceledException) throw;
          else
            throw Failure(exception);
        }
      }
    }
  }

  private RuntimeException Failure(Exception exception, CypherErrorCode code = CypherErrorCode.ProcedureFailed)
  {
    var error = new RuntimeException($"Procedure '{Call.Registration.QualifiedName}' failed: {exception.Message}", exception, code);
    error.Locate(Call.SourceSpan);
    return error;
  }

  private void ValidateResult(IRow row, RowSchema columns)
  {
    if (row == null || row.Values == null || row.Columns == null || row.Values.Length != columns.Count || row.Columns.Count != columns.Count)
      throw new InvalidProcedureResultException("Result row does not match the declared output width.");
    for (var i = 0; i < columns.Count; i++)
    {
      if (row.Columns[i] == null || row.Columns[i].Index != i || row.Columns[i].Name != columns[i].Name || row.Columns[i].Type != columns[i].Type)
        throw new InvalidProcedureResultException($"Result column {i} does not match the declared output schema.");
      if (!ProcedureRegistration.Accepts(Call.Registration.Outputs[i], row.Values[i] ?? MixedValue.Null()))
        throw new InvalidProcedureResultException($"Result column '{columns[i].Name}' has an invalid type or null value.");
    }
  }

  private sealed class InvalidProcedureResultException(string message) : Exception(message);
}
