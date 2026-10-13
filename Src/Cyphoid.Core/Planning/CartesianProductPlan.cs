using System.Text;
using Cyphoid.Core.Execution;

namespace Cyphoid.Core.Planning
{
  public record CartesianProductPlan(
    PipelinePlan Left,
    PipelinePlan Right) : PipelinePlan
  {
    public override IOperator BuildExecutionPlan(IOperatorFactory factory)
    {
      return factory.BuildCartesianProduct(
        Left.BuildExecutionPlan(factory),
        Right.BuildExecutionPlan(factory));
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      sb.AppendLine("Cartesian product");
      // FIXME: Indents
      Left.PrettyPrint(sb);
      Right.PrettyPrint(sb);
    }
  }
}
