using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Core.Execution
{
  public interface IOperator<TId> where TId : IEquatable<TId>
  {
    IAsyncEnumerable<IRow<TId>> ExecuteAsync(IQueryContext context);
  }
}
