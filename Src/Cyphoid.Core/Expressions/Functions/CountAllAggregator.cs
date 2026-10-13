using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Core.Expressions.Functions
{
  public record CountAllAggregator(
    int SlotIndex) : IAggregationEvaluator
  {
    int Count = 0;

    void IAggregationEvaluator.Initialize()
    {
      Count = 0;
    }


    void IAggregationEvaluator.Accumulate(IRow row)
    {
      Count++;
    }


    void IAggregationEvaluator.WriteResult(IRow row)
    {
      row.Values[SlotIndex] = MixedValue.Int(Count);
    }
  }
}
