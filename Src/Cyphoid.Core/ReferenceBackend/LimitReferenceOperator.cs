using System.Runtime.CompilerServices;
using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public record LimitReferenceOperator(
    IOperator Input,
    int Limit) : IOperator
  {
    async IAsyncEnumerable<IRow> IOperator.ExecuteAsync(IQueryContext context, [EnumeratorCancellation] CancellationToken ct)
    {
      ct.ThrowIfCancellationRequested();
      await foreach (var row in Input.ExecuteAsync(context, ct).Take(Limit).WithCancellation(ct))
      {
        ct.ThrowIfCancellationRequested();
        yield return row;
      }
    }
  }
}
