using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record MatchWhereNode(PatternNode MatchPattern, ExprNode? WhereExpr) : AstNode
  {
    public PipelinePlan<TId> BuildQueryPlan<TId>(
      PipelinePlan<TId>? input,
      IRowColumn[] columns) where TId : IEquatable<TId>
    {
      var plan = MatchPattern.BuildPlan<TId>(input, columns);
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
