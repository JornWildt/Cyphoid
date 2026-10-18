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
        var materialization = new AsyncMaterialization(factory, parameters);
        var expressions = Projections.Select(p => materialization.Transform(p.Expr)).ToArray();

        var groupings = new List<GroupingEvaluator>();
        var aggregators = new List<IAggregationEvaluator>();

      foreach (var p in Projections)
        {
          var expression = expressions[projections.Count];
          var evaluator = expression.CompileEvaluator();
          projections.Add(new ProjectionEvaluator(evaluator, p.Variable));

          if (p.Expr.ValueKind == ValueKindType.Aggregate)
            aggregators.Add(expression.GetAggregationEvaluator(evaluator, p.Variable.SlotIndex));
          else if (!p.IsImported)
            groupings.Add(new GroupingEvaluator(evaluator, p.Variable.SlotIndex));
        }

        IOperator Build(IOperator source) => aggregators.Count > 0
          ? factory.BuildAggregationProjection(source, groupings, aggregators, outputColumns)
          : factory.BuildProjection(source, new ProjectionBindings(projections, outputColumns));
        if (aggregators.Count > 0)
          foreach (var projection in Projections.Where(p => p.IsImported))
            aggregators.Add(new ImportedConstantAggregator(projection.Expr.Bind(parameters).CompileEvaluator(), projection.Variable.SlotIndex));
        var source = input(parameters);
        return materialization.Evaluators.Count == 0 ? Build(source)
          : new AsyncStageOperator(source, materialization.Evaluators, Build, false);
      };
    }

    private sealed record ImportedConstantAggregator(RowEvaluator Evaluate, int Slot) : IAggregationEvaluator
    {
      public void Initialize() { }
      public void Accumulate(IRow row) { }
      public void WriteResult(IRow row) => row.Values[Slot] = Evaluate(new Row(RowSchema.Empty));
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
