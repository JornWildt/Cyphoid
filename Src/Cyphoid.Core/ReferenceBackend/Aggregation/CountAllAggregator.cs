using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.ReferenceBackend.Aggregation
{
  public record CountAllAggregator<TId>(
    int SlotIndex) : IAggregationEvaluator<TId> where TId : IEquatable<TId>
  {
    int Count = 0;

    void IAggregationEvaluator<TId>.Initialize()
    {
      Count = 0;
    }


    void IAggregationEvaluator<TId>.Accumulate(IRow<TId> row)
    {
      Count++;
    }


    void IAggregationEvaluator<TId>.WriteResult(IRow<TId> row)
    {
      row.Values[SlotIndex] = MixedValue.Int(Count);
    }
  }
}
