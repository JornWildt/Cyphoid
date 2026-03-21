using System.Text;
using Cyphoid.Core.Execution;

namespace Cyphoid.Core.Planning
{
  public record LimitPlan<TId>(
    PipelinePlan<TId> Input,
    int Limit) : PipelinePlan<TId> where TId : IEquatable<TId>
  {
    public override IOperator<TId> BuildExecutionPlan(IOperatorFactory<TId> factory)
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
