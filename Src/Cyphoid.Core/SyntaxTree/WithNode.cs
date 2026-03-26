using System.Text;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record WithNode(
    IReadOnlyList<ProjectionNode> Projections) : ClauseNode()
  {
    public override PipelinePlan<TId> BuildQueryPlan<TId>(
      PipelinePlan<TId>? input)
    {
      input = input ?? new EmptyPlan<TId>();
      input = new ProjectionPlan<TId>(input, Projections);

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
