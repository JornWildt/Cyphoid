using System.Runtime.CompilerServices;
using Cyphoid.Core.Exceptions;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;
using Cyphoid.Core.Procedures;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.ReferenceBackend;

internal sealed record ProcedureCallReferenceOperator(
  IOperator Input, CallNode Call, IReadOnlyList<RowEvaluator> Arguments) : IOperator
{
  public IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context) => Execute(context);

  private async IAsyncEnumerable<IRow> Execute(
    IQueryContext context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    var registration = Call.Registration;
    var columns = Array.AsReadOnly(registration.Outputs
      .Select((f, i) => (IRowColumn)new RowColumn(i, f.Name, f.Type)).ToArray());
    await foreach (var input in Input.ExecuteAsync(context).WithCancellation(cancellationToken))
    {
      var arguments = Arguments.Select(a => a(input)).ToArray();
      for (var i = 0; i < arguments.Length; i++)
        if (!ProcedureRegistration.Accepts(registration.Parameters[i], arguments[i]))
          throw new RuntimeException($"Procedure '{registration.QualifiedName}' argument '{registration.Parameters[i].Name}' requires {registration.Parameters[i].Type} (nullable: {registration.Parameters[i].Nullable}); got {arguments[i].Type}.");

      // Wrap both synchronous invocation errors and deferred enumeration errors.
      IAsyncEnumerator<IRow> results;
      try
      {
        results = registration.Procedure.ExecuteAsync(context, input, Array.AsReadOnly(arguments),
          columns, cancellationToken).GetAsyncEnumerator(cancellationToken);
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
            if (!await results.MoveNextAsync()) break;
            result = results.Current;
            ValidateResult(result, columns);
          }
          catch (Exception exception) when (exception is not OperationCanceledException)
          {
            throw Failure(exception);
          }

          // MergeWith uses shared query slots, not concatenation. Leave input
          // slots unset in the right row so the host merge preserves their values.
          var yielded = context.RowFactory.EmptyRow(result, Call.OutputColumns);
          foreach (var binding in Call.Yields)
            yielded.Values[binding.Variable.SlotIndex] = result.Values[binding.OutputIndex] ?? MixedValue.Null();
          yield return input.MergeWith(yielded);
        }
      }
      finally
      {
        try { await results.DisposeAsync(); }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
          throw Failure(exception);
        }
      }
    }
  }

  private RuntimeException Failure(Exception exception) =>
    new($"Procedure '{Call.Registration.QualifiedName}' failed: {exception.Message}", exception);

  private void ValidateResult(IRow row, IReadOnlyList<IRowColumn> columns)
  {
    if (row == null || row.Values.Length != columns.Count || row.Columns.Length != columns.Count)
      throw new InvalidOperationException("Result row does not match the declared output width.");
    for (var i = 0; i < columns.Count; i++)
    {
      if (row.Columns[i].Index != i || row.Columns[i].Name != columns[i].Name || row.Columns[i].Type != columns[i].Type)
        throw new InvalidOperationException($"Result column {i} does not match the declared output schema.");
      if (!ProcedureRegistration.Accepts(Call.Registration.Outputs[i], row.Values[i] ?? MixedValue.Null()))
        throw new InvalidOperationException($"Result column '{columns[i].Name}' has an invalid type or null value.");
    }
  }
}
