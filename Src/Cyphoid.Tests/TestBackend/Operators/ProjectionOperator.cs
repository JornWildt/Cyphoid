using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Tests.TestBackend.Operators
{
  internal class ProjectionOperator : OperatorBase, IProjectionOperator
  {
    IOperator Input;
    IReadOnlyList<ProjectionEvaluator> Projections;


    public ProjectionOperator(
      InMemoryGraph graph, 
      IOperator input,
      IReadOnlyList<ProjectionEvaluator> projections)
      : base(graph)
    {
      Input = input;
      Projections = projections;
    }

    
    async IAsyncEnumerable<Dictionary<string, object?>> IProjectionOperator.ExecuteAsync(QueryContext context)
    {
      await foreach (var row in Input.ExecuteAsync(context))
      {
        var output = new Dictionary<string, object?>();
        foreach (var p in Projections)
        {
          var value = p.ExpressionEvaluator(row);
          var name = p.OutputName;
          output[name] = value.AsObject(); 
        }
        yield return output;
      }
    }
  }
}
