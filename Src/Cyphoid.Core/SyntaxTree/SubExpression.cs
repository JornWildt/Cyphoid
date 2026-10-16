using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Core.SyntaxTree
{
  public record SubExpression(ExprNode Sub)
    : ExprNode(Sub.ValueKind)
  {
    public override RowEvaluator BuildEvaluator()
    {
      return Sub.CompileEvaluator();
    }


    public override IAggregationEvaluator GetAggregationEvaluator(RowEvaluator expression, int outputSlotIndex)
    {
      return Sub.GetAggregationEvaluator(expression, outputSlotIndex);
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("(");
      Sub.PrettyPrint(sb);
      sb.Append(")");
    }
  }
}
