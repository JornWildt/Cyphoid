using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Core.Expressions.Functions
{
  public record MinAggregator(
    IReadOnlyList<RowEvaluator> Parameters,
    int SlotIndex) : IAggregationEvaluator
  {
    MixedValue? MinValue = null;

    void IAggregationEvaluator.Initialize()
    {
      MinValue = null;
    }


    void IAggregationEvaluator.Accumulate(IRow row)
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


    void IAggregationEvaluator.WriteResult(IRow row)
    {
      row.Values[SlotIndex] = MinValue != null ? MinValue : MixedValue.Null();
    }
  }
}
