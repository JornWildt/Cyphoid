using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Core.Execution
{
  public interface IProjectionOperator
  {
    IAsyncEnumerable<Dictionary<string,object?>> ExecuteAsync(QueryContext context);
  }
}
