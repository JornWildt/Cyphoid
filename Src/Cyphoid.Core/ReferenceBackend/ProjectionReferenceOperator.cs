using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public class ProjectionReferenceOperator<TId> : IOperator<TId> where TId : IEquatable<TId>
  {
    IOperator<TId> Input;
    IReadOnlyList<ProjectionEvaluator<TId>> Projections;
    IRowColumn[] Columns;

    public ProjectionReferenceOperator(
      IOperator<TId> input,
      IReadOnlyList<ProjectionEvaluator<TId>> projections)
    {
      Input = input;
      Projections = projections;

      Columns = projections
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
