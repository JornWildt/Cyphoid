using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Expressions.Functions
{
  internal class RandFunction : IFunctionDefinition
  {
    ValueKindType IFunctionDefinition.ValueKind => ValueKindType.Variable;


    RowEvaluator<TId> IFunctionDefinition.GetRowEvaluator<TId>(
      IReadOnlyList<RowEvaluator<TId>> parameters)
    {
      return (IRow<TId> row) => MixedValue.Double(Random.Shared.NextDouble());
    }


    IAggregationEvaluator<TId> IFunctionDefinition.GetAggregator<TId>(
      IReadOnlyList<RowEvaluator<TId>> parameters,
      int outputSlotIndex)
    {
      throw new NotSupportedException("rand() is a scalar function.");
    }
  }
}
