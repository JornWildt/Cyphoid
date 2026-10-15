using Cyphoid.Core.Exceptions;
using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Expressions.Functions
{
  internal class RandFunction : IFunctionDefinition
  {
    ValueKindType IFunctionDefinition.ValueKind => ValueKindType.Variable;


    RowEvaluator IFunctionDefinition.GetRowEvaluator(
      IReadOnlyList<RowEvaluator> parameters)
    {
      if (parameters.Count != 0)
        throw new ParseException("rand() requires no arguments.");

      return (IRow row) => MixedValue.Double(Random.Shared.NextDouble());
    }


    IAggregationEvaluator IFunctionDefinition.GetAggregator(
      IReadOnlyList<RowEvaluator> parameters,
      int outputSlotIndex)
    {
      throw new NotSupportedException("rand() is a scalar function.");
    }
  }
}
