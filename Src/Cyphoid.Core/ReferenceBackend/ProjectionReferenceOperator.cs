using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public class ProjectionReferenceOperator<TId> : IProjectionOperator where TId : IEquatable<TId>
  {
    IOperator<TId> Input;
    IReadOnlyList<ProjectionEvaluator<TId>> Projections;


    public ProjectionReferenceOperator(
      IOperator<TId> input,
      IReadOnlyList<ProjectionEvaluator<TId>> projections)
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
