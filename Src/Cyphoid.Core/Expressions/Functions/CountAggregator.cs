using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Core.Expressions.Functions
{
  public record CountAggregator<TId>(
    IReadOnlyList<RowEvaluator<TId>> Parameters,
    int SlotIndex) : IAggregationEvaluator<TId> where TId : IEquatable<TId>
  {
    int? Count = null;

    void IAggregationEvaluator<TId>.Initialize()
    {
      Count = null;
    }


    void IAggregationEvaluator<TId>.Accumulate(IRow<TId> row)
    {
      var rowValue = Parameters[0](row);
      if (!rowValue.IsNull())
      {
        Count = Count == null ? 1 : Count + 1;
      }
    }


    void IAggregationEvaluator<TId>.WriteResult(IRow<TId> row)
    {
      row.Values[SlotIndex] = Count != null ? MixedValue.Int(Count.Value) : MixedValue.Null();
    }
  }
}
