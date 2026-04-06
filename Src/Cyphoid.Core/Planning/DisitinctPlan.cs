using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning
{
  public record DistinctPlan<TId>(
    PipelinePlan<TId> Input,
    IReadOnlyList<ProjectionNode> Projections) : PipelinePlan<TId> where TId : IEquatable<TId>
  {
    public override IOperator<TId> BuildExecutionPlan(IOperatorFactory<TId> factory)
    {
      var groupings = new List<GroupingEvaluator<TId>>();
      foreach (var p in Projections)
      {
        var varExpr = new VariableExprNode(p.Variable);
        var evaluator = varExpr.BuildEvaluator<TId>();
        groupings.Add(new GroupingEvaluator<TId>(evaluator, p.Variable.SlotIndex));
      }

      return factory.BuildDistinct(
        Input.BuildExecutionPlan(factory),
        groupings);
    }

    
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.AppendLine("Distinct");
      Input.PrettyPrint(sb);
    }
  }
}
