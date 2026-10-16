using System.Runtime.CompilerServices;
using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public record EmptyResultReferenceOperator() : IOperator
  {
    IAsyncEnumerable<IRow> IOperator.ExecuteAsync(IQueryContext context, CancellationToken ct) => ExecuteAsync(context, ct);


#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    
    protected virtual async IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context, [EnumeratorCancellation] CancellationToken ct = default)
    {
      ct.ThrowIfCancellationRequested();
      yield return context.RowFactory.EmptyRow([]);
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
  }
}
