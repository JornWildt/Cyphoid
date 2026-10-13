using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public record LimitReferenceOperator(
    IOperator Input,
    int Limit) : IOperator
  {
    IAsyncEnumerable<IRow> IOperator.ExecuteAsync(IQueryContext context)
    {
      return Input.ExecuteAsync(context).Take(Limit);
    }
  }
}
