using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record QueryNode(
    IReadOnlyList<ClauseNode> Clauses,
    ReturnLimitNode ReturnLimit) : AstNode
  {
    public PipelinePlan<TId> BuildQueryPlan<TId>() where TId : IEquatable<TId>
    {
      PipelinePlan<TId>? plan = null;
      foreach (var c in Clauses)
      {
        plan = c.BuildQueryPlan(plan);
      }
      
      plan = plan ?? new EmptyPlan<TId>();

      var projectionPlan = ReturnLimit.BuildProjectionPlan(plan);
      return projectionPlan;
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      var first = true;
      foreach (var c in Clauses)
      {
        if (!first)
          sb.Append(" ");
        c.PrettyPrint(sb);
        first = false;
      }

      if (!first)
        sb.Append(" ");

      ReturnLimit.PrettyPrint(sb);
    }
  }
}
