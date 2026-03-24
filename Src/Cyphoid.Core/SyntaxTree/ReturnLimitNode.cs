using System.Text;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record ReturnLimitNode(
    IReadOnlyList<ReturnProjectionNode> ReturnItems, 
    OrderByNode? OrderBy,
    int? Limit) : AstNode
  {
    public PipelinePlan<TId> BuildProjectionPlan<TId>(PipelinePlan<TId> input) where TId : IEquatable<TId>
    {
      input = new ProjectionPlan<TId>(input, ReturnItems);

      if (OrderBy != null)
        input = new OrderByPlan<TId>(input, OrderBy.Ordering);

      if (Limit != null)
        input = new LimitPlan<TId>(input, Limit.Value);

      return input;
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("RETURN ");
      bool first = true;
      foreach (var i in ReturnItems)
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
