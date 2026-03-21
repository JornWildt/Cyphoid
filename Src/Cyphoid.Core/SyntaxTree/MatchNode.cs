using System.Text;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record MatchNode(PatternNode Pattern) : AstNode
  {
    public PipelinePlan<TId> BuildQueryPlan<TId>() where TId : IEquatable<TId>
    {
      return Pattern.BuildPlan<TId>();
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("MATCH ");
      Pattern.PrettyPrint(sb);
    }
  }
}
