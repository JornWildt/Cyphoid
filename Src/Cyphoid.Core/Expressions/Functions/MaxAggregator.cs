using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Core.Expressions.Functions
{
  public record MaxAggregator(
    IReadOnlyList<RowEvaluator> Parameters,
    int SlotIndex) : IAggregationEvaluator
  {
    MixedValue? MaxValue = null;

    void IAggregationEvaluator.Initialize()
    {
      MaxValue = null;
    }


    void IAggregationEvaluator.Accumulate(IRow row)
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


    void IAggregationEvaluator.WriteResult(IRow row)
    {
      // FIXME: Not correct
      row.Values[SlotIndex] = MaxValue != null ? MaxValue : MixedValue.Null();
    }
  }
}
