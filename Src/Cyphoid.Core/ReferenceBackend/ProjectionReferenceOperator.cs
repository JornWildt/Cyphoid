using System.Security.Cryptography;
using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public class ProjectionReferenceOperator<TId>(
    IOperator<TId> Input,
    IReadOnlyList<ProjectionEvaluator<TId>> Projections,
    IRowColumn[] Columns) : IOperator<TId> where TId : IEquatable<TId>
  {
    public ProjectionReferenceOperator(
      IOperator<TId> input,
      IReadOnlyList<ProjectionEvaluator<TId>> projections)
      : this(input, projections, MakeColumns(projections))
    {
    }


    private static IRowColumn[] MakeColumns(IReadOnlyList<ProjectionEvaluator<TId>> projections)
    {
      return projections
        .Select((p, i) => new RowColumn(i, p.OutputVariable.Name, p.OutputVariable.Type))
        .ToArray();
    }


    async IAsyncEnumerable<IRow<TId>> IOperator<TId>.ExecuteAsync(IQueryContext context)
    {
      await foreach (var row in Input.ExecuteAsync(context))
      {
        IRow<TId> newRow = new Row<TId>(Columns);

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
