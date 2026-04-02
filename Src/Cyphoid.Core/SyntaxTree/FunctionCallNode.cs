using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions.Functions;
using Cyphoid.Core.ReferenceBackend.Aggregation;

namespace Cyphoid.Core.SyntaxTree
{
  public record FunctionCallNode(
    string FunctionName,
    IFunctionDefinition Definition,
    IReadOnlyList<ExprNode> Parameters) : ExprNode(Definition.ValueKind)
  {
    public override RowEvaluator<TId> BuildEvaluator<TId>()
    {
      var parameters = Parameters.Select(p => p.BuildEvaluator<TId>()).ToArray();
      return Definition.GetRowEvaluator<TId>(parameters);
    }


    public override IAggregationEvaluator<TId> GetAggregationEvaluator<TId>(
      RowEvaluator<TId> expression,
      int outputSlotIndex)
    {
      var parameters = Parameters.Select(p => p.BuildEvaluator<TId>()).ToArray();
      return Definition.GetAggregator(parameters, outputSlotIndex);
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      if (Definition is CountAllFunction)
      {
        sb.Append("COUNT(*)");
      }
      else
      {
        sb.Append(FunctionName);
        sb.Append("(");
        bool first = true;
        foreach (var p in Parameters)
        {
          if (!first)
            sb.Append(", ");
          p.PrettyPrint(sb);
          first = false;
        }
        sb.Append(")");
      }
    }
  }
}
