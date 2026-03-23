using System.Text;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record QueryNode(
    IReadOnlyList<MatchWhereNode> MatchWhere,
    ReturnLimitNode ReturnLimit,
    Dictionary<string, VariableDefinition> VariableDefinitions,
    int RowSize) : AstNode
  {
    public ProjectionPlan<TId> BuildQueryPlan<TId>() where TId : IEquatable<TId>
    {
      PipelinePlan<TId>? plan = null;
      foreach (var mw in MatchWhere)
      {
        plan = mw.BuildQueryPlan<TId>(plan);
      }
      
      plan = plan ?? new EmptyPlan<TId>();

      var projectionPlan = ReturnLimit.BuildQueryPlan(plan);
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
