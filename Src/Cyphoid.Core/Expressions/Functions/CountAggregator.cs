using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Core.Expressions.Functions
{
  public record CountAggregator(
    IReadOnlyList<RowEvaluator> Parameters,
    int SlotIndex) : IAggregationEvaluator
  {
    int Count = 0;

    void IAggregationEvaluator.Initialize()
    {
      Count = 0;
    }


    void IAggregationEvaluator.Accumulate(IRow row)
    {
      var rowValue = Parameters[0](row);
      if (!rowValue.IsNull())
      {
        Count++;
      }
    }


    void IAggregationEvaluator.WriteResult(IRow row)
    {
      row.Values[SlotIndex] = MixedValue.Int(Count);
    }
  }
}
