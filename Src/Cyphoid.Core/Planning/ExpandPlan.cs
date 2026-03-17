using System.Text;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning
{
  public record ExpandPlan(
    LogicalPlan Input,
    NodeVariable Source,
    NodeVariable Destination,
    RelationshipPatternNode Relationship) : LogicalPlan
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.AppendLine("Expand");
      Input.PrettyPrint(sb);
    }
  }
}
