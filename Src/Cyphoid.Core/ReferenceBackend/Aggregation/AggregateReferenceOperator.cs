using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend.Aggregation
{
  public record GroupingEvaluator<TId>(
    RowEvaluator<TId> Expression) where TId : IEquatable<TId>
  {

  }


  public interface IAggregationEvaluator<TId> where TId : IEquatable<TId>
  {
    void Accumulate(IRow<TId> row);
    void WriteResult(IRow<TId> row);
  }

  public record AggregateReferenceOperator<TId>(
    IOperator<TId> Input,
    IReadOnlyList<GroupingEvaluator<TId>> Groupings,
    IReadOnlyList<IAggregationEvaluator<TId>> Aggregators,
    Func<IRow<TId>> RowFactory) : IOperator<TId> where TId : IEquatable<TId>
  {
    async IAsyncEnumerable<IRow<TId>> IOperator<TId>.ExecuteAsync(IQueryContext context)
    {
      var result = Input.ExecuteAsync(context);

      var groups = result.GroupBy(r => new GroupKey<TId>(r, Groupings));

      await foreach (var group in groups)
      {
        foreach (var row in group)
        {
          if (group != null)
          {
            foreach (var a in Aggregators)
            {
              a.Accumulate(row);
            }
          }
        }

        var groupRow = RowFactory();
        foreach (var a in Aggregators)
        {
          a.WriteResult(groupRow);
        }

        yield return groupRow;
      }

    }
  }
}
