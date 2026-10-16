using System.Runtime.CompilerServices;
using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public record OptionalMatchReferenceOperator(
    IOperator Input, Func<IRow, IOperator> BuildMatch, RowSchema Columns) : IOperator
  {
    public async IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context, [EnumeratorCancellation] CancellationToken ct = default)
    {
      ct.ThrowIfCancellationRequested();
      await foreach (var row in Input.ExecuteAsync(context, ct).WithCancellation(ct))
      {
        ct.ThrowIfCancellationRequested();
        bool matched = false;
        await foreach (var result in BuildMatch(row).ExecuteAsync(context, ct).WithCancellation(ct))
        {
          ct.ThrowIfCancellationRequested();
          matched = true;
          yield return result;
        }
        ct.ThrowIfCancellationRequested();
        if (!matched)
          yield return row.CopyAndExpand(Columns);
      }
    }
  }
}
