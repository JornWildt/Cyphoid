using System.Text;
using Cyphoid.Core.Execution;

namespace Cyphoid.Core.Planning
{
  public record LimitPlan(
    LogicalPlan Input,
    long Limit) : PipelinePlan
  {
    public override IOperator BuildExecutionPlan(IOperatorFactory factory)
    {
      throw new NotImplementedException();
    }

    public override void PrettyPrint(StringBuilder sb)
    {
      sb.AppendLine("Limit");
      Input.PrettyPrint(sb);
    }
  }
}
