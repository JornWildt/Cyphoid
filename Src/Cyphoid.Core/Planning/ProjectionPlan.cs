using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning
{
  public record ProjectionPlan(
    PipelinePlan Input,
    IReadOnlyList<ProjectionNode> Projections) : PipelinePlan
  {
    public override Func<QueryParameters, IOperator> Compile(IOperatorFactory factory)
    {
      var input = Input.Compile(factory);
      var outputColumns = new RowSchema(Projections.Select((p, i) => new RowColumn(i, p.Variable.Name, p.Variable.Type)));
      return parameters =>
      {
        var projections = new List<ProjectionEvaluator>();

        var groupings = new List<GroupingEvaluator>();
        var aggregators = new List<IAggregationEvaluator>();

        foreach (var p in Projections)
        {
          var expression = p.Expr.Bind(parameters);
          var evaluator = expression.CompileEvaluator();
          projections.Add(new ProjectionEvaluator(evaluator, p.Variable));

          if (p.Expr.ValueKind == ValueKindType.Aggregate)
            aggregators.Add(expression.GetAggregationEvaluator(evaluator, p.Variable.SlotIndex));
          else
            groupings.Add(new GroupingEvaluator(evaluator, p.Variable.SlotIndex));
        }

        if (aggregators.Count > 0)
        {
          return factory.BuildAggregationProjection(
            input(parameters),
            groupings,
            aggregators,
            outputColumns);
        }
        else
        {
          return factory.BuildProjection(
            input(parameters),
            new ProjectionBindings(projections, outputColumns));
        }
      };
    }

    
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("Project: ");
      bool first = true;
      foreach (var i in Projections)
      {
        if (!first)
          sb.Append(", ");
        i.Expr.PrettyPrint(sb);
        if (!i.Variable.IsAnonymous)
          sb.Append(" AS " + i.Variable.Name);
      }
      sb.AppendLine();
      Input.PrettyPrint(sb);
    }
  }
}
