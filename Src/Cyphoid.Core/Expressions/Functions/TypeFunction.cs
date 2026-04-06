using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Expressions.Functions
{
  internal class TypeFunction : IFunctionDefinition
  {
    ValueKindType IFunctionDefinition.ValueKind => ValueKindType.Variable;


    RowEvaluator<TId> IFunctionDefinition.GetRowEvaluator<TId>(
      IReadOnlyList<RowEvaluator<TId>> parameters)
    {
      return (IRow<TId> row) =>
      {
        var value = parameters[0](row);
        if (value.TryGetGraphNode(out var node))
        {
          return MixedValue.String(node.Type);
        }
        return MixedValue.Null();
      };
    }


    IAggregationEvaluator<TId> IFunctionDefinition.GetAggregator<TId>(
      IReadOnlyList<RowEvaluator<TId>> parameters,
      int outputSlotIndex)
    {
      return new CountAggregator<TId>(parameters, outputSlotIndex);
    }
  }
}
