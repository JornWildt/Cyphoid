using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Core.ReferenceBackend.Aggregation;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning
{
  public record ProjectionPlan<TId>(
    PipelinePlan<TId> Input,
    IReadOnlyList<ReturnProjectionNode> Projections) : PipelinePlan<TId> where TId : IEquatable<TId>
  {
    public override IOperator<TId> BuildExecutionPlan(IOperatorFactory<TId> factory)
    {
      var projections = new List<ProjectionEvaluator<TId>>();

      var groupings = new List<GroupingEvaluator<TId>>();
      var aggregators = new List<IAggregationEvaluator<TId>>();

      foreach (var p in Projections)
      {
        var evaluator = p.Expr.BuildEvaluator<TId>();
        projections.Add(new ProjectionEvaluator<TId>(evaluator, p.Variable));

        if (p.Expr.ValueKind == ValueKindType.Aggregate)
          aggregators.Add(p.Expr.GetAggregationEvaluator(evaluator, p.Variable.SlotIndex));
        else
          groupings.Add(new GroupingEvaluator<TId>(evaluator, p.Variable.SlotIndex));
      }

      if (aggregators.Count > 0)
      {
        var outputColumns = Projections
          .Select((p, i) => new RowColumn(i, p.Variable.Name, p.Variable.Type))
          .ToArray();

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
