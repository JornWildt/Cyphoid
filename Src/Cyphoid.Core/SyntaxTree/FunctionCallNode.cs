using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions.Functions;
using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Core.SyntaxTree
{
  public record FunctionCallNode(
    string FunctionName,
    IFunctionDefinition Definition,
    IReadOnlyList<ExprNode> Parameters) : ExprNode(Definition.ValueKind)
  {
    public override RowEvaluator BuildEvaluator()
    {
      var parameters = Parameters.Select(p => p.BuildEvaluator()).ToArray();
      return Definition.GetRowEvaluator(parameters);
    }


    public override IAggregationEvaluator GetAggregationEvaluator(
      RowEvaluator expression,
      int outputSlotIndex)
    {
      var parameters = Parameters.Select(p => p.BuildEvaluator()).ToArray();
      return Definition.GetAggregator(parameters, outputSlotIndex);
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      if (Definition is CountAllFunction)
      {
        sb.Append(FunctionName);
        sb.Append("(*)");
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
