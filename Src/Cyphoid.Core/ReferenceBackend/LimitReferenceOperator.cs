using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public record LimitReferenceOperator<TId>(
    IOperator<TId> Input,
    int Limit) : IOperator<TId> where TId : IEquatable<TId>
  {
    IAsyncEnumerable<IRow<TId>> IOperator<TId>.ExecuteAsync(IQueryContext context)
    {
      return Input.ExecuteAsync(context).Take(Limit);
    }
  }
}
