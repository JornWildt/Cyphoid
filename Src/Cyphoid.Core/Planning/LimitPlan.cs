using System.Text;

namespace Cyphoid.Core.Planning
{
  public record LimitPlan(
    LogicalPlan Input,
    long Limit) : LogicalPlan
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.AppendLine("Limit");
      Input.PrettyPrint(sb);
    }
  }
}
