using Cyphoid.Core.Execution;

namespace Cyphoid.Tests.TestBackend.Operators
{
  internal class LimitOperator : OperatorBase, IOperator
  {
    IOperator Input;
    int Limit;

    public LimitOperator(
      InMemoryGraph graph,
      IOperator input,
      int limit)
      : base(graph)
    {
      Input = input;
      Limit = limit;
    }


    IAsyncEnumerable<Row> IOperator.ExecuteAsync(QueryContext context)
    {
      return Input.ExecuteAsync(context).Take(Limit);
    }
  }
}
