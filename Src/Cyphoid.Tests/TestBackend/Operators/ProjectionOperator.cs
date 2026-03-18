using Cyphoid.Core.Execution;

namespace Cyphoid.Tests.TestBackend.Operators
{
  internal class ProjectionOperator : OperatorBase, IProjectionOperator
  {
    IOperator Input;
    
    
    public ProjectionOperator(
      InMemoryGraph graph, 
      IOperator input)
      : base(graph)
    {
      Input = input;
    }

    
    async IAsyncEnumerable<Dictionary<string, object?>> IProjectionOperator.ExecuteAsync(QueryContext context)
    {
      await foreach (var row in Input.ExecuteAsync(context))
      {
        yield return new Dictionary<string, object?>();
      }
    }
  }
}
