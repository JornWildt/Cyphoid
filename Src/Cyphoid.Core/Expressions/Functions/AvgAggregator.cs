using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Core.Expressions.Functions
{
  public record AvgAggregator<TId>(
    IReadOnlyList<RowEvaluator<TId>> Parameters,
    int SlotIndex) : IAggregationEvaluator<TId> where TId : IEquatable<TId>
  {
    MixedValue? SumValue = null;
    int Counter = 0;

    void IAggregationEvaluator<TId>.Initialize()
    {
      SumValue = null;
      Counter = 0;
    }


    void IAggregationEvaluator<TId>.Accumulate(IRow<TId> row)
    {
      var rowValue = Parameters[0](row);
      if (!rowValue.IsNull())
      {
        if (SumValue == null)
          SumValue = rowValue;
        else
          SumValue = SumValue + rowValue;
        Counter++;
      }
    }


    void IAggregationEvaluator<TId>.WriteResult(IRow<TId> row)
    {
      row.Values[SlotIndex] = Counter > 0 ? SumValue / MixedValue.Int(Counter) : MixedValue.Null();
    }
  }
}
