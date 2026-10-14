using Cyphoid.Core.Exceptions;
using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Expressions.Functions
{
  internal class IdFunction : IFunctionDefinition
  {
    ValueKindType IFunctionDefinition.ValueKind => ValueKindType.Variable;

    RowEvaluator IFunctionDefinition.GetRowEvaluator(IReadOnlyList<RowEvaluator> parameters)
    {
      if (parameters.Count != 1)
        throw new ParseException("id() requires exactly one argument.");

      return (IRow row) =>
      {
        var value = parameters[0](row);
        if (value.IsNull())
          return MixedValue.Null();
        if (value.TryGetGraphNode(out var node))
          return node.GetId();

        throw new RuntimeException($"id() requires a node or null. Got {value.Type}.");
      };
    }

    IAggregationEvaluator IFunctionDefinition.GetAggregator(
      IReadOnlyList<RowEvaluator> parameters, int outputSlotIndex)
    {
      throw new NotSupportedException("id() is a scalar function.");
    }
  }
}
