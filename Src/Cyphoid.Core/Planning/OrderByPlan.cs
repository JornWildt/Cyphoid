using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;
using Cyphoid.Core.ReferenceBackend;

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
        var materialization = new AsyncMaterialization(factory, parameters);
        var expressions = Ordering.Select(o => materialization.Transform(o.Expression)).ToArray();

        foreach (var o in Ordering)
        {
          var evaluator = expressions[ordering.Count].CompileEvaluator();
          var direction = o.Direction == OrderByDirectionType.Descending ? SortDirection.Descending : SortDirection.Ascending;
          ordering.Add(new OrderByEvaluator(evaluator, direction));
        }

        var source = input(parameters);
        return materialization.Evaluators.Count == 0 ? factory.BuildOrderBy(source, ordering)
          : new AsyncStageOperator(source, materialization.Evaluators, rows => factory.BuildOrderBy(rows, ordering), true);
      };
    }

    
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.AppendLine("ORDER");
    }
  }
}
