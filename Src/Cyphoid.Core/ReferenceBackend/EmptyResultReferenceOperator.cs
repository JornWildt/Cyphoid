using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public class EmptyResultReferenceOperator<TId> : IOperator<TId> where TId : IEquatable<TId>
  {
    IAsyncEnumerable<IRow<TId>> IOperator<TId>.ExecuteAsync(IQueryContext context) => ExecuteAsync(context);


#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    
    protected virtual async IAsyncEnumerable<IRow<TId>> ExecuteAsync(IQueryContext context)
    {
      yield return new Row<TId>([]);
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
  }
}
