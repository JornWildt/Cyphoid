using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;

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
