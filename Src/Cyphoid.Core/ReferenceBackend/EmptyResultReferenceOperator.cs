using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public class EmptyResultReferenceOperator : IOperator
  {
    IAsyncEnumerable<IRow> IOperator.ExecuteAsync(IQueryContext context) => ExecuteAsync(context);

    protected virtual async IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context)
    {
      yield return new Row(0);
    }
  }
}
