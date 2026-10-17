using System.Runtime.CompilerServices;
using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning
{
  public record OptionalMatchPlan(
    PipelinePlan Input, PatternNode Pattern, ExprNode? Predicate, RowSchema Columns) : PipelinePlan
  {
    public override Func<QueryParameters, IOperator> Compile(IOperatorFactory factory)
    {
      var input = Input.Compile(factory);
      return parameters =>
      {
        var pattern = Pattern.Bind(parameters);
        var predicate = Predicate?.Bind(parameters);
        return new OptionalMatchReferenceOperator(input(parameters), row =>
        {
          // Match the whole clause normally; only the enclosing operator supplies null fallback rows.
          var plan = pattern.BuildPlan(new InputRowPlan(row), Columns);
          if (predicate != null)
            plan = new FilterPlan(plan, predicate);
          return plan.Compile(factory)(parameters);
        }, Columns);
      };
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
      public override Func<QueryParameters, IOperator> Compile(IOperatorFactory factory) => _ => new InputRowOperator(Row);
      public override void PrettyPrint(StringBuilder sb) => sb.AppendLine("Input row");
    }

    private record InputRowOperator(IRow Row) : IOperator
    {
      public async IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context, [EnumeratorCancellation] CancellationToken ct = default)
      {
        ct.ThrowIfCancellationRequested();
        await Task.CompletedTask;
        yield return Row;
      }
    }
  }
}
