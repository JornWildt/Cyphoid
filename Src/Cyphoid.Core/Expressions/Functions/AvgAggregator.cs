using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Core.Expressions.Functions
{
  public record AvgAggregator(
    IReadOnlyList<RowEvaluator> Parameters,
    int SlotIndex) : IAggregationEvaluator
  {
    MixedValue? SumValue = null;
    int Counter = 0;

    void IAggregationEvaluator.Initialize()
    {
      SumValue = null;
      Counter = 0;
    }


    void IAggregationEvaluator.Accumulate(IRow row)
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


    void IAggregationEvaluator.WriteResult(IRow row)
    {
      row.Values[SlotIndex] = Counter > 0 ? SumValue / MixedValue.Int(Counter) : MixedValue.Null();
    }
  }
}
