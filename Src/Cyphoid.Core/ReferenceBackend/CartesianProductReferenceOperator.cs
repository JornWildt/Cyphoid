using System.Runtime.CompilerServices;
using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public record CartesianProductReferenceOperator(
    IOperator Left,
    IOperator Right) : IOperator
  {
    async IAsyncEnumerable<IRow> IOperator.ExecuteAsync(IQueryContext context, [EnumeratorCancellation] CancellationToken ct)
    {
      ct.ThrowIfCancellationRequested();
      await foreach (var row1 in Left.ExecuteAsync(context, ct).WithCancellation(ct))
      {
        ct.ThrowIfCancellationRequested();
        await foreach (var row2 in Right.ExecuteAsync(context, ct).WithCancellation(ct))
        {
          ct.ThrowIfCancellationRequested();
          var output = row1.MergeWith(row2);
          yield return output;
        }
      }
    }
  }
}
