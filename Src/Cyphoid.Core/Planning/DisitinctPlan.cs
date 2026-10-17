using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning
{
  public record DistinctPlan(
    PipelinePlan Input,
    IReadOnlyList<ProjectionNode> Projections) : PipelinePlan
  {
    public override Func<QueryParameters, IOperator> Compile(IOperatorFactory factory)
    {
      var input = Input.Compile(factory);
      var groupings = new List<GroupingEvaluator>();
      foreach (var p in Projections)
      {
        var varExpr = new VariableExprNode(p.Variable);
        var evaluator = varExpr.CompileEvaluator();
        groupings.Add(new GroupingEvaluator(evaluator, p.Variable.SlotIndex));
      }

      return parameters => factory.BuildDistinct(
        input(parameters),
        groupings);
    }

    
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.AppendLine("Distinct");
      Input.PrettyPrint(sb);
    }
  }
}
