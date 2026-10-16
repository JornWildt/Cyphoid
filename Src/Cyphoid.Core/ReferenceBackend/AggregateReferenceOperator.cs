using System.Runtime.CompilerServices;
using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public record GroupingEvaluator(
    RowEvaluator Expression,
    int OutputSlotIndex)
  {

  }


  public interface IAggregationEvaluator
  {
    void Initialize();
    void Accumulate(IRow row);
    void WriteResult(IRow row);
  }

  public record AggregateReferenceOperator(
    IOperator Input,
    IReadOnlyList<GroupingEvaluator> Groupings,
    IReadOnlyList<IAggregationEvaluator> Aggregators,
    RowSchema OutputColumns) : IOperator
  {
    async IAsyncEnumerable<IRow> IOperator.ExecuteAsync(IQueryContext context, [EnumeratorCancellation] CancellationToken ct)
    {
      ct.ThrowIfCancellationRequested();
      var result = Input.ExecuteAsync(context, ct);

      var groups = result.GroupBy(r =>
      {
        ct.ThrowIfCancellationRequested();
        return new GroupKey(r, Groupings);
      });
      var hasGroups = false;

      await foreach (var group in groups.WithCancellation(ct))
      {
        ct.ThrowIfCancellationRequested();
        hasGroups = true;
        foreach (var a in Aggregators)
        {
          a.Initialize();
        }

        foreach (var row in group)
        {
          ct.ThrowIfCancellationRequested();
          foreach (var a in Aggregators)
          {
            a.Accumulate(row);
          }
        }

        var groupRow = context.RowFactory.EmptyRow(OutputColumns);
        foreach (var a in Aggregators)
        {
          a.WriteResult(groupRow);
        }

        group.Key.WriteGroupData(groupRow);

        yield return groupRow;
      }

      // A global aggregate has one group even when its input is empty.
      ct.ThrowIfCancellationRequested();
      if (!hasGroups && Groupings.Count == 0)
      {
        var groupRow = context.RowFactory.EmptyRow(OutputColumns);
        foreach (var a in Aggregators)
        {
          a.Initialize();
          a.WriteResult(groupRow);
        }
        yield return groupRow;
      }
    }
  }
}
