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
    public override IOperator BuildExecutionPlan(IOperatorFactory factory)
    {
      var projections = new List<ProjectionEvaluator>();

      var groupings = new List<GroupingEvaluator>();
      var aggregators = new List<IAggregationEvaluator>();

      foreach (var p in Projections)
      {
        var evaluator = p.Expr.CompileEvaluator();
        projections.Add(new ProjectionEvaluator(evaluator, p.Variable));

        if (p.Expr.ValueKind == ValueKindType.Aggregate)
          aggregators.Add(p.Expr.GetAggregationEvaluator(evaluator, p.Variable.SlotIndex));
        else
          groupings.Add(new GroupingEvaluator(evaluator, p.Variable.SlotIndex));
      }

      if (aggregators.Count > 0)
      {
        var outputColumns = new RowSchema(Projections
          .Select((p, i) => new RowColumn(i, p.Variable.Name, p.Variable.Type)));

        return factory.BuildAggregationProjection(
          Input.BuildExecutionPlan(factory),
          groupings,
          aggregators,
          outputColumns);
      }
      else
      {
        return factory.BuildProjection(
          Input.BuildExecutionPlan(factory),
          projections);
      }
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
