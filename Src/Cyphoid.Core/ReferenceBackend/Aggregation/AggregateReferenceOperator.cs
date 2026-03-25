using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend.Aggregation
{
  public record GroupingEvaluator<TId>(
    RowEvaluator<TId> Expression,
    int OutputSlotIndex) where TId : IEquatable<TId>
  {

  }


  public interface IAggregationEvaluator<TId> where TId : IEquatable<TId>
  {
    void Initialize();
    void Accumulate(IRow<TId> row);
    void WriteResult(IRow<TId> row);
  }

  public record AggregateReferenceOperator<TId>(
    IOperator<TId> Input,
    IReadOnlyList<GroupingEvaluator<TId>> Groupings,
    IReadOnlyList<IAggregationEvaluator<TId>> Aggregators,
    Func<IRowColumn[], IRow<TId>> RowFactory,
    IRowColumn[] OutputColumns) : IOperator<TId> where TId : IEquatable<TId>
  {
    async IAsyncEnumerable<IRow<TId>> IOperator<TId>.ExecuteAsync(IQueryContext context)
    {
      var result = Input.ExecuteAsync(context);

      var groups = result.GroupBy(r => new GroupKey<TId>(r, Groupings));

      await foreach (var group in groups)
      {
        foreach (var a in Aggregators)
        {
          a.Initialize();
        }

        foreach (var row in group)
        {
          foreach (var a in Aggregators)
          {
            a.Accumulate(row);
          }
        }

        var groupRow = RowFactory(OutputColumns);
        foreach (var a in Aggregators)
        {
          a.WriteResult(groupRow);
        }

        group.Key.WriteGroupData(groupRow);

        yield return groupRow;
      }

    }
  }
}
