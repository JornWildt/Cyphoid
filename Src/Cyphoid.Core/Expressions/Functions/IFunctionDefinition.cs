using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend.Aggregation;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Expressions.Functions
{
  public interface IFunctionDefinition
  {
    ValueKindType ValueKind { get; }
    
    RowEvaluator<TId> GetRowEvaluator<TId>(
      IReadOnlyList<RowEvaluator<TId>> parameters) where TId : IEquatable<TId>;
    
    IAggregationEvaluator<TId> GetAggregator<TId>(
      IReadOnlyList<RowEvaluator<TId>> parameters,
      int outputSlotIndex) where TId : IEquatable<TId>;
  }
}
