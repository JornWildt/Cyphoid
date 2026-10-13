using System.Text;
using Cyphoid.Core.Execution;

namespace Cyphoid.Core.Planning
{
  public record LimitPlan(
    PipelinePlan Input,
    int Limit) : PipelinePlan
  {
    public override IOperator BuildExecutionPlan(IOperatorFactory factory)
    {
      return factory.BuildLimit(Input.BuildExecutionPlan(factory), Limit);
    }

    public override void PrettyPrint(StringBuilder sb)
    {
      sb.AppendLine("Limit");
      Input.PrettyPrint(sb);
    }
  }
}
