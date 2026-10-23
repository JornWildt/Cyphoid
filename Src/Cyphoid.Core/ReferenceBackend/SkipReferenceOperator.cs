using System.Runtime.CompilerServices;
using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend;

public sealed record SkipReferenceOperator(IOperator Input, int Skip) : IOperator
{
  public async IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context,
    [EnumeratorCancellation] CancellationToken ct = default)
  {
    ct.ThrowIfCancellationRequested();
    var remaining = Skip;
    await foreach (var row in Input.ExecuteAsync(context, ct).WithCancellation(ct))
    {
      // Discarding rows can do substantial work without yielding to the consumer.
      ct.ThrowIfCancellationRequested();
      if (remaining > 0) { remaining--; continue; }
      yield return row; // Preserve the host row, metadata, and shared schema.
    }
  }
}
