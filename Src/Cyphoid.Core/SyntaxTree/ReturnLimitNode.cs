using System.Text;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record ReturnLimitNode(
    bool Distinct,
    IReadOnlyList<ProjectionNode> Projections, 
    OrderByNode? OrderBy,
    ExprNode? Limit,
    IReadOnlyList<ProjectionNode>? OrderingProjections = null) : AstNode
  {
    public PipelinePlan BuildProjectionPlan(PipelinePlan input)
    {
      var hasHiddenProjections = OrderingProjections is { Count: > 0 };

      input = new ProjectionPlan(input, hasHiddenProjections
        ? Projections.Concat(OrderingProjections!).ToArray()
        : Projections);

      if (Distinct)
        input = new DistinctPlan(input, Projections);

      if (OrderBy != null)
        input = new OrderByPlan(input, OrderBy.Ordering);

      if (Limit != null)
        input = new LimitPlan(input, Limit);

      if (hasHiddenProjections)
        input = new ProjectionPlan(input, Projections
          .Select(p => new ProjectionNode(new VariableExprNode(p.Variable), p.Variable))
          .ToArray());

      return input;
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("RETURN ");
      if (Distinct)
        sb.Append("DISTINCT ");

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
        sb.Append(" LIMIT ");
        Limit.PrettyPrint(sb);
      }
    }
  }
}
