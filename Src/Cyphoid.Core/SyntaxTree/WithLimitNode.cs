using Cyphoid.Core.Planning;
using System.Text;

namespace Cyphoid.Core.SyntaxTree
{
  public record WithLimitNode(
    IReadOnlyList<ProjectionNode> Projections,
    OrderByNode? OrderBy,
    int? Limit) : ClauseNode()
  {
    public override PipelinePlan BuildQueryPlan(
      PipelinePlan? input)
    {
      input = input ?? new EmptyPlan();

      input = new ProjectionPlan(input, Projections);

      if (OrderBy != null)
        input = new OrderByPlan(input, OrderBy.Ordering);

      if (Limit != null)
        input = new LimitPlan(input, Limit.Value);

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

      if (OrderBy != null)
      {
        sb.Append(" ");
        OrderBy.PrettyPrint(sb);
      }

      if (Limit != null)
      {
        sb.Append(" LIMIT " + Limit);
      }
    }
  }
}
