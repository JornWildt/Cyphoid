using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Expressions.Functions
{
  public interface IFunctionDefinition
  {
    ValueKindType ValueKind { get; }
    
    RowEvaluator GetRowEvaluator(
      IReadOnlyList<RowEvaluator> parameters);
    
    IAggregationEvaluator GetAggregator(
      IReadOnlyList<RowEvaluator> parameters,
      int outputSlotIndex);
  }
}
