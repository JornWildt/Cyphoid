using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Core.Expressions.Functions
{
  public record CountAggregator(
    IReadOnlyList<RowEvaluator> Parameters,
    int SlotIndex) : IAggregationEvaluator
  {
    int? Count = null;

    void IAggregationEvaluator.Initialize()
    {
      Count = null;
    }


    void IAggregationEvaluator.Accumulate(IRow row)
    {
      var rowValue = Parameters[0](row);
      if (!rowValue.IsNull())
      {
        Count = Count == null ? 1 : Count + 1;
      }
    }


    void IAggregationEvaluator.WriteResult(IRow row)
    {
      row.Values[SlotIndex] = Count != null ? MixedValue.Int(Count.Value) : MixedValue.Null();
    }
  }
}
