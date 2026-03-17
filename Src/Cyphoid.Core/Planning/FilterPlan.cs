using System.Text;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning
{
  public record FilterPlan(
    LogicalPlan Input,
    ExprNode Predicate) : LogicalPlan
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("Filter: ");
      Predicate.PrettyPrint(sb);
      sb.AppendLine();
      Input.PrettyPrint(sb);
    }
  }
}
