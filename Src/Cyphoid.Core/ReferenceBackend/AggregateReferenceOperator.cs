using Cyphoid.Core.Execution;
using System.Security.Cryptography;

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
    Func<IRowColumn[], IRow> RowFactory,
    IRowColumn[] OutputColumns) : IOperator
  {
    async IAsyncEnumerable<IRow> IOperator.ExecuteAsync(IQueryContext context)
    {
      var result = Input.ExecuteAsync(context);

      var groups = result.GroupBy(r => new GroupKey(r, Groupings));

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
