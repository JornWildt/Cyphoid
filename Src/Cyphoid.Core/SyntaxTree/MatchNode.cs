using System.Text;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record MatchNode(PatternNode Pattern) : AstNode
  {
    public LogicalPlan BuildQueryPlan()
    {
      return Pattern.BuildPlan();
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("MATCH ");
      Pattern.PrettyPrint(sb);
    }
  }
}
