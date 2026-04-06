using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Core.Expressions.Functions
{
  public record MinAggregator<TId>(
    IReadOnlyList<RowEvaluator<TId>> Parameters,
    int SlotIndex) : IAggregationEvaluator<TId> where TId : IEquatable<TId>
  {
    MixedValue? MinValue = null;

    void IAggregationEvaluator<TId>.Initialize()
    {
      MinValue = null;
    }


    void IAggregationEvaluator<TId>.Accumulate(IRow<TId> row)
    {
      var rowValue = Parameters[0](row);
      if (!rowValue.IsNull())
      {
        if (MinValue == null)
          MinValue = rowValue;
        else
          MinValue = (rowValue < MinValue.Value).AsBool() ? rowValue : MinValue;
      }
    }


    void IAggregationEvaluator<TId>.WriteResult(IRow<TId> row)
    {
      row.Values[SlotIndex] = MinValue != null ? MinValue : MixedValue.Null();
    }
  }
}
