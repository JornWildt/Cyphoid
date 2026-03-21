using System.Text;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record QueryNode(
    IReadOnlyList<MatchWhereNode> MatchWhere,
    ReturnLimitNode ReturnLimit,
    Dictionary<string, VariableDefinition> VariableDefinitions) : AstNode
  {
    public int RowSize => VariableDefinitions.Count;


    public ProjectionPlan<TId> BuildQueryPlan<TId>() where TId : IEquatable<TId>
    {
      var plan = MatchWhere.Count > 0 ? MatchWhere[0].BuildQueryPlan<TId>() : new EmptyPlan<TId>();
      foreach (var mw in MatchWhere.Skip(1))
      {
        var nextPlan = mw.BuildQueryPlan<TId>();
        // FIXME: Join the plans
      }
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
