using System.Runtime.CompilerServices;
using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public class ProjectionReferenceOperator(
    IOperator Input,
    IReadOnlyList<ProjectionEvaluator> Projections,
    IRowColumn[] Columns) : IOperator
  {
    public ProjectionReferenceOperator(
      IOperator input,
      IReadOnlyList<ProjectionEvaluator> projections)
      : this(input, projections, MakeColumns(projections))
    {
    }


    private static IRowColumn[] MakeColumns(IReadOnlyList<ProjectionEvaluator> projections)
    {
      return projections
        .Select((p, i) => new RowColumn(i, p.OutputVariable.Name, p.OutputVariable.Type))
        .ToArray();
    }


    async IAsyncEnumerable<IRow> IOperator.ExecuteAsync(IQueryContext context, [EnumeratorCancellation] CancellationToken ct)
    {
      ct.ThrowIfCancellationRequested();
      await foreach (var row in Input.ExecuteAsync(context, ct).WithCancellation(ct))
      {
        ct.ThrowIfCancellationRequested();
        IRow newRow = context.RowFactory.EmptyRow(row, Columns);

        foreach (var p in Projections)
        {
          var value = p.ExpressionEvaluator(row);
          newRow.Values[p.OutputVariable.SlotIndex] = value;
        }

        yield return newRow;
      }
    }
  }
}
