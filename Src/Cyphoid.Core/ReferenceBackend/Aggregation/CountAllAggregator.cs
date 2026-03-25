using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.ReferenceBackend.Aggregation
{
  internal class CountAllAggregator<TId> : IAggregationEvaluator<TId> where TId : IEquatable<TId>
  {
    int Count = 0;
    int SlotIndex;

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
