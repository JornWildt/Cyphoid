using Cyphoid.Core.Execution;

namespace Cyphoid.Tests.TestBackend.Operators
{
  internal class ProjectionOperator : OperatorBase, IOperator
  {
    IOperator Input;
    
    
    public ProjectionOperator(InMemoryGraph graph, IOperator input)
      : base(graph)
    {
      Input = input;
    }

    
    async IAsyncEnumerable<Row> IOperator.ExecuteAsync(QueryContext context)
    {
      await foreach (var row in Input.ExecuteAsync(context))
      {
        yield return row;
      }
    }
  }
}
