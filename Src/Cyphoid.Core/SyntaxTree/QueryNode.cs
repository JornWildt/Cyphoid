using System.Text;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record QueryNode(
    MatchNode Match, 
    WhereNode? Where, 
    ReturnNode Return, 
    LimitNode? Limit,
    Dictionary<string, VariableDefinition> VariableDefinitions) : AstNode
  {
    public int RowSize => VariableDefinitions.Count;


    public ProjectionPlan BuildQueryPlan()
    {
      PipelinePlan plan = Match.BuildQueryPlan();

      if (Where != null)
      {
        plan = Where.BuildQueryPlan(plan);
      }

      if (Limit != null)
        plan = Limit.BuildQueryPlan(plan);

      var projectionPlan = Return.BuildQueryPlan(plan);

      return projectionPlan;
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      Match.PrettyPrint(sb);
      if (Where != null)
      {
        sb.Append(" ");
        Where.PrettyPrint(sb);
      }
      sb.Append(" ");
      Return.PrettyPrint(sb);
      if (Limit != null)
      {
        sb.Append(" ");
        Limit.PrettyPrint(sb);
      }
    }
  }
}
