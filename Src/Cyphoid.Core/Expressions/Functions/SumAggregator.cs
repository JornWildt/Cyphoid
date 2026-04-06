using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Core.Expressions.Functions
{
  public record SumAggregator<TId>(
    IReadOnlyList<RowEvaluator<TId>> Parameters,
    int SlotIndex) : IAggregationEvaluator<TId> where TId : IEquatable<TId>
  {
    MixedValue? Sum = null;

    void IAggregationEvaluator<TId>.Initialize()
    {
      Sum = null;
    }


    void IAggregationEvaluator<TId>.Accumulate(IRow<TId> row)
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


    void IAggregationEvaluator<TId>.WriteResult(IRow<TId> row)
    {
      row.Values[SlotIndex] = Sum != null ? Sum : MixedValue.Null();
    }
  }
}
