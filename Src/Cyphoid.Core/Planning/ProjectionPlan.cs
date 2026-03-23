using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning
{
  public record ProjectionPlan<TId>(
    PipelinePlan<TId> Input,
    IReadOnlyList<ReturnProjectionNode> Projections) : LogicalPlan<TId> where TId : IEquatable<TId>
  {
    public IOperator<TId> BuildExecutionPlan(IOperatorFactory<TId> factory)
    {
      var projections = new List<ProjectionEvaluator<TId>>();

      foreach (var p in Projections)
      {
        var evaluator = p.Expr.BuildEvaluator<TId>();
        projections.Add(new ProjectionEvaluator<TId>(evaluator, p.Variable));
      }
      
      return factory.BuildProjection(
        Input.BuildExecutionPlan(factory), 
        projections);
    }

    
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("Project: ");
      bool first = true;
      foreach (var i in Projections)
      {
        if (!first)
          sb.Append(", ");
        i.Expr.PrettyPrint(sb);
        if (!i.Variable.IsAnonymous)
          sb.Append(" AS " + i.Variable.Name);
      }
      sb.AppendLine();
      Input.PrettyPrint(sb);
    }
  }
}
