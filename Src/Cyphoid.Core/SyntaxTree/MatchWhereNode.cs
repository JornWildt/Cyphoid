using System.Text;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record MatchWhereNode(PatternNode MatchPattern, ExprNode? WhereExpr) : AstNode
  {
    public PipelinePlan<TId> BuildQueryPlan<TId>() where TId : IEquatable<TId>
    {
      var plan = MatchPattern.BuildPlan<TId>();
      if (WhereExpr != null)
      {
        plan = new FilterPlan<TId>(plan, WhereExpr);
      }
      return plan;
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("MATCH ");
      MatchPattern.PrettyPrint(sb);

      if (WhereExpr != null)
      {
        sb.Append(" WHERE ");
        WhereExpr.PrettyPrint(sb);
      }
    }
  }
}
