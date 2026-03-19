using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public class LimitReferenceOperator : IOperator
  {
    IOperator Input;
    int Limit;


    public LimitReferenceOperator(
      IOperator input,
      int limit)
    {
      Input = input;
      Limit = limit;
    }


    IAsyncEnumerable<IRow> IOperator.ExecuteAsync(IQueryContext context)
    {
      return Input.ExecuteAsync(context).Take(Limit);
    }
  }
}
