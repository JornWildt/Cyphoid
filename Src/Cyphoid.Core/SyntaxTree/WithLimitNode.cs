using Cyphoid.Core.Planning;
using System.Text;

namespace Cyphoid.Core.SyntaxTree
{
  public record WithLimitNode(
    IReadOnlyList<ProjectionNode> Projections,
    OrderByNode? OrderBy,
    ExprNode? Limit,
    ExprNode? Predicate = null,
    ExprNode? Skip = null) : ClauseNode()
  {
    public override PipelinePlan BuildQueryPlan(
      PipelinePlan? input)
    {
      input = input ?? new EmptyPlan();

      input = new ProjectionPlan(input, Projections);

      if (OrderBy != null)
        input = new OrderByPlan(input, OrderBy.Ordering);

      if (Skip != null)
        input = new SkipPlan(input, Skip);

      if (Limit != null)
        input = new LimitPlan(input, Limit);

      if (Predicate != null)
        input = new FilterPlan(input, Predicate);

      return input;
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("WITH ");
      bool first = true;
      foreach (var i in Projections.Where(p => !p.IsImplicit))
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

      if (Skip != null)
      {
        sb.Append(" SKIP ");
        Skip.PrettyPrint(sb);
      }

      if (Limit != null)
      {
        sb.Append(" LIMIT ");
        Limit.PrettyPrint(sb);
      }
      if (Predicate != null)
      {
        sb.Append(" WHERE ");
        Predicate.PrettyPrint(sb);
      }
    }
  }
}
