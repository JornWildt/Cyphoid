using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record MatchWhereNode(
    bool Optional,
    PatternNode MatchPattern, 
    ExprNode? WhereExpr,
    RowSchema DeclaredColumns) : ClauseNode
  {
    public override PipelinePlan BuildQueryPlan(
      PipelinePlan? input)
    {
      if (Optional)
        return new OptionalMatchPlan(input ?? new EmptyPlan(), MatchPattern, WhereExpr, DeclaredColumns);

      var plan = MatchPattern.BuildPlan(input, DeclaredColumns);
      if (WhereExpr != null)
      {
        plan = new FilterPlan(plan, WhereExpr);
      }
      return plan;
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      if (Optional)
        sb.Append("OPTIONAL ");

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
