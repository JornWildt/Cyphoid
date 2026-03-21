using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public class LimitReferenceOperator<TId> : IOperator<TId> where TId : IEquatable<TId>
  {
    IOperator<TId> Input;
    int Limit;


    public LimitReferenceOperator(
      IOperator<TId> input,
      int limit)
    {
      Input = input;
      Limit = limit;
    }


    IAsyncEnumerable<IRow<TId>> IOperator<TId>.ExecuteAsync(IQueryContext context)
    {
      return Input.ExecuteAsync(context).Take(Limit);
    }
  }
}
