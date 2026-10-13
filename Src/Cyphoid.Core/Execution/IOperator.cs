using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Core.Execution
{
  public interface IOperator
  {
    IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context);
  }
}
