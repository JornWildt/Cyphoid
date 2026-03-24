using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record QueryNode(
    IReadOnlyList<MatchWhereNode> MatchWhere,
    IRowColumn[] MatchColumns,
    ReturnLimitNode ReturnLimit) : AstNode
  {
    public PipelinePlan<TId> BuildQueryPlan<TId>() where TId : IEquatable<TId>
    {
      PipelinePlan<TId>? plan = null;
      foreach (var mw in MatchWhere)
      {
        plan = mw.BuildQueryPlan(plan, MatchColumns);
      }
      
      plan = plan ?? new EmptyPlan<TId>();

      var projectionPlan = ReturnLimit.BuildProjectionPlan(plan);
      return projectionPlan;
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      var first = true;
      foreach (var mw in MatchWhere)
      {
        if (!first)
          sb.Append(" ");
        mw.PrettyPrint(sb);
        first = false;
      }

      if (!first)
        sb.Append(" ");

      ReturnLimit.PrettyPrint(sb);
    }
  }
}
