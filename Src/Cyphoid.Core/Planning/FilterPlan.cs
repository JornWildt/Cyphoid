using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning
{
  public record FilterPlan(
    PipelinePlan Input,
    ExprNode Predicate) : PipelinePlan
  {
    public override IOperator BuildExecutionPlan(IOperatorFactory factory)
    {
      var evaluator = Predicate.BuildEvaluator();
      return factory.BuildFilter(
        Input.BuildExecutionPlan(factory), 
        evaluator);
    }

    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("Filter: ");
      Predicate.PrettyPrint(sb);
      sb.AppendLine();
      Input.PrettyPrint(sb);
    }
  }
}
