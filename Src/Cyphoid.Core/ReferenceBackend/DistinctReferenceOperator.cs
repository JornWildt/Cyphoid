using System.Runtime.CompilerServices;
using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public record DistinctReferenceOperator(
    IOperator Input,
    IReadOnlyList<GroupingEvaluator> Groupings) : IOperator
  {
    async IAsyncEnumerable<IRow> IOperator.ExecuteAsync(IQueryContext context, [EnumeratorCancellation] CancellationToken ct)
    {
      ct.ThrowIfCancellationRequested();
      var rows = Input.ExecuteAsync(context, ct).DistinctBy(r =>
      {
        ct.ThrowIfCancellationRequested();
        return new GroupKey(r, Groupings);
      });
      await foreach (var row in rows.WithCancellation(ct))
      {
        ct.ThrowIfCancellationRequested();
        yield return row;
      }
    }
  }
}
