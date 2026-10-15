using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning
{
  public record OptionalMatchPlan(
    PipelinePlan Input, PatternNode Pattern, ExprNode? Predicate, IRowColumn[] Columns) : PipelinePlan
  {
    public override IOperator BuildExecutionPlan(IOperatorFactory factory)
    {
      return new OptionalMatchReferenceOperator(Input.BuildExecutionPlan(factory), row =>
      {
        // Match the whole clause normally; only the enclosing operator supplies null fallback rows.
        var plan = Pattern.BuildPlan(new InputRowPlan(row), Columns);
        if (Predicate != null)
          plan = new FilterPlan(plan, Predicate);
        return plan.BuildExecutionPlan(factory);
      }, Columns);
    }

    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("Optional match: ");
      Pattern.PrettyPrint(sb);
      if (Predicate != null)
      {
        sb.Append(" WHERE ");
        Predicate.PrettyPrint(sb);
      }
      sb.AppendLine();
      Input.PrettyPrint(sb);
    }

    private record InputRowPlan(IRow Row) : PipelinePlan
    {
      public override IOperator BuildExecutionPlan(IOperatorFactory factory) => new InputRowOperator(Row);
      public override void PrettyPrint(StringBuilder sb) => sb.AppendLine("Input row");
    }

    private record InputRowOperator(IRow Row) : IOperator
    {
      public async IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context)
      {
        await Task.CompletedTask;
        yield return Row;
      }
    }
  }
}
