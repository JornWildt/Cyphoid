using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public class EmptyResultReferenceOperator : IOperator
  {
    IAsyncEnumerable<IRow> IOperator.ExecuteAsync(IQueryContext context) => ExecuteAsync(context);


#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    
    protected virtual async IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context)
    {
      yield return new Row(0);
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
  }
}
