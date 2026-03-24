using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning
{
  public record OrderByPlan<TId>(
    PipelinePlan<TId> Input,
    IReadOnlyList<OrderByItemNode> Ordering) : PipelinePlan<TId> where TId : IEquatable<TId>
  {
    public override IOperator<TId> BuildExecutionPlan(IOperatorFactory<TId> factory)
    {
      var ordering = new List<OrderByEvaluator<TId>>();

      foreach (var o in Ordering)
      {
        var evaluator = o.Expression.BuildEvaluator<TId>();
        var direction = o.Direction == OrderByDirectionType.Descending ? SortDirection.Descending : SortDirection.Ascending;
        ordering.Add(new OrderByEvaluator<TId>(evaluator, direction));
      }

      return factory.BuildOrderBy(Input.BuildExecutionPlan(factory), ordering);
    }

    
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.AppendLine("ORDER");
    }
  }
}
