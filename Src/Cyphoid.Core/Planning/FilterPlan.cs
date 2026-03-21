using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning
{
  public record FilterPlan<TId>(
    PipelinePlan<TId> Input,
    ExprNode Predicate) : PipelinePlan<TId> where TId : IEquatable<TId>
  {
    public override IOperator<TId> BuildExecutionPlan(IOperatorFactory<TId> factory)
    {
      var evaluator = Predicate.BuildEvaluator<TId>();
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
