using Cyphoid.Core.Exceptions;
using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Expressions.Functions
{
  public class CountAllFunction : IFunctionDefinition
  {
    ValueKindType IFunctionDefinition.ValueKind => ValueKindType.Aggregate;
    void IFunctionDefinition.ValidateArguments(int count)
    {
      if (count != 0)
        throw new ParseException("countall() requires 0 arguments.", CypherErrorCode.InvalidArgumentCount);
    }

    
    RowEvaluator IFunctionDefinition.GetRowEvaluator(
      IReadOnlyList<RowEvaluator> parameters)
    {
      ((IFunctionDefinition)this).ValidateArguments(parameters.Count);
      return (IRow row) => throw new InvalidOperationException("Aggregate expressions must be evaluated by an aggregation operator.");
    }

    
    IAggregationEvaluator IFunctionDefinition.GetAggregator(
      IReadOnlyList<RowEvaluator> parameters,
      int outputSlotIndex)
    {
      ((IFunctionDefinition)this).ValidateArguments(parameters.Count);
      return new CountAllAggregator(outputSlotIndex);
    }
  }
}
