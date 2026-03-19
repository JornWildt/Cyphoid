using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public class ProjectionReferenceOperator : IProjectionOperator
  {
    IOperator Input;
    IReadOnlyList<ProjectionEvaluator> Projections;


    public ProjectionReferenceOperator(
      IOperator input,
      IReadOnlyList<ProjectionEvaluator> projections)
    {
      Input = input;
      Projections = projections;
    }


    async IAsyncEnumerable<IDictionary<string, object?>> IProjectionOperator.ExecuteAsync(IQueryContext context)
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
