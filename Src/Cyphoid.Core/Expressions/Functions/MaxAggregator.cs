using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Core.Expressions.Functions
{
  public record MaxAggregator<TId>(
    IReadOnlyList<RowEvaluator<TId>> Parameters,
    int SlotIndex) : IAggregationEvaluator<TId> where TId : IEquatable<TId>
  {
    MixedValue? MaxValue = null;

    void IAggregationEvaluator<TId>.Initialize()
    {
      MaxValue = null;
    }


    void IAggregationEvaluator<TId>.Accumulate(IRow<TId> row)
    {
      var rowValue = Parameters[0](row);
      if (!rowValue.IsNull())
      {
        if (MaxValue == null)
          MaxValue = rowValue;
        else
          MaxValue = (rowValue > MaxValue.Value).AsBool() ? rowValue : MaxValue;
      }
    }


    void IAggregationEvaluator<TId>.WriteResult(IRow<TId> row)
    {
      // FIXME: Not correct
      row.Values[SlotIndex] = MaxValue != null ? MaxValue : MixedValue.Null();
    }
  }
}
