using System.Runtime.CompilerServices;
using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public record FilterReferenceOperator(
    IOperator Input,
    RowEvaluator PredicateEvaluator) : IOperator
  {
    async IAsyncEnumerable<IRow> IOperator.ExecuteAsync(IQueryContext context, [EnumeratorCancellation] CancellationToken ct)
    {
      ct.ThrowIfCancellationRequested();
      await foreach (var row in Input.ExecuteAsync(context, ct).WithCancellation(ct))
      {
        ct.ThrowIfCancellationRequested();
        var ok = PredicateEvaluator(row);
        if (!ok.IsAnythingButTrue())
          yield return row;
      }
    }
  }
}
