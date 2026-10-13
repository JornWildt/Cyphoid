using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Core.Expressions.Functions
{
  public record SumAggregator(
    IReadOnlyList<RowEvaluator> Parameters,
    int SlotIndex) : IAggregationEvaluator
  {
    MixedValue? Sum = null;

    void IAggregationEvaluator.Initialize()
    {
      Sum = null;
    }


    void IAggregationEvaluator.Accumulate(IRow row)
    {
      var rowValue = Parameters[0](row);
      if (!rowValue.IsNull())
      {
        if (Sum == null)
          Sum = rowValue;
        else
          Sum = Sum + rowValue;
      }
    }


    void IAggregationEvaluator.WriteResult(IRow row)
    {
      row.Values[SlotIndex] = Sum != null ? Sum : MixedValue.Null();
    }
  }
}
