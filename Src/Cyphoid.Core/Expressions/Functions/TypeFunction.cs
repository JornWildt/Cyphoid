using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Expressions.Functions
{
  internal class TypeFunction : IFunctionDefinition
  {
    ValueKindType IFunctionDefinition.ValueKind => ValueKindType.Variable;


    RowEvaluator IFunctionDefinition.GetRowEvaluator(
      IReadOnlyList<RowEvaluator> parameters)
    {
      return (IRow row) =>
      {
        var value = parameters[0](row);
        if (value.TryGetGraphNode(out var node))
        {
          return MixedValue.String(node.Type);
        }
        return MixedValue.Null();
      };
    }


    IAggregationEvaluator IFunctionDefinition.GetAggregator(
      IReadOnlyList<RowEvaluator> parameters,
      int outputSlotIndex)
    {
      throw new NotSupportedException("type() is a scalar function.");
    }
  }
}
