using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend.Aggregation;

namespace Cyphoid.Core.SyntaxTree
{
  public record SubExpression(ExprNode Sub)
    : ExprNode(Sub.ValueKind)
  {
    public override RowEvaluator<TId> BuildEvaluator<TId>()
    {
      return Sub.BuildEvaluator<TId>();
    }


    public override IAggregationEvaluator<TId> GetAggregationEvaluator<TId>(RowEvaluator<TId> expression, int outputSlotIndex)
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
