using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend.Aggregation;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Expressions.Functions
{
  public class CountAllFunction : IFunctionDefinition
  {
    ValueKindType IFunctionDefinition.ValueKind => ValueKindType.Aggregate;

    
    RowEvaluator<TId> IFunctionDefinition.GetRowEvaluator<TId>(
      IReadOnlyList<RowEvaluator<TId>> parameters)
    {
      return (IRow<TId> row) => MixedValue.Int(-1);
    }

    
    IAggregationEvaluator<TId> IFunctionDefinition.GetAggregator<TId>(
      IReadOnlyList<RowEvaluator<TId>> parameters,
      int outputSlotIndex)
    {
      return new CountAllAggregator<TId>(outputSlotIndex);
    }
  }
}
