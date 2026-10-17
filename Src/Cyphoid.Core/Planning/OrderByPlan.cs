using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning
{
  public record OrderByPlan(
    PipelinePlan Input,
    IReadOnlyList<OrderByItemNode> Ordering) : PipelinePlan
  {
    public override Func<QueryParameters, IOperator> Compile(IOperatorFactory factory)
    {
      var input = Input.Compile(factory);
      return parameters =>
      {
        var ordering = new List<OrderByEvaluator>();

        foreach (var o in Ordering)
        {
          var evaluator = o.Expression.Bind(parameters).CompileEvaluator();
          var direction = o.Direction == OrderByDirectionType.Descending ? SortDirection.Descending : SortDirection.Ascending;
          ordering.Add(new OrderByEvaluator(evaluator, direction));
        }

        return factory.BuildOrderBy(input(parameters), ordering);
      };
    }

    
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.AppendLine("ORDER");
    }
  }
}
