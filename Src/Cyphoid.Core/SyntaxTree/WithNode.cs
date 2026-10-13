using System.Text;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record WithNode(
    IReadOnlyList<ProjectionNode> Projections) : ClauseNode()
  {
    public override PipelinePlan BuildQueryPlan(
      PipelinePlan? input)
    {
      input = input ?? new EmptyPlan();
      input = new ProjectionPlan(input, Projections);

      return input;
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("WITH ");
      bool first = true;
      foreach (var i in Projections)
      {
        if (!first)
          sb.Append(", ");
        i.PrettyPrint(sb);
        first = false;
      }
    }
  }
}
