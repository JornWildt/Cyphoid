using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record MatchWhereNode(
    PatternNode MatchPattern, 
    ExprNode? WhereExpr,
    IRowColumn[] DeclaredColumns) : ClauseNode
  {
    public override PipelinePlan<TId> BuildQueryPlan<TId>(
      PipelinePlan<TId>? input)
    {
      var plan = MatchPattern.BuildPlan<TId>(input, DeclaredColumns);
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
