using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.SyntaxTree
{
  public enum BinaryOperatorType { And, Or }

  public record BinaryOperatorNode(ExprNode Left, ExprNode Right, BinaryOperatorType Operator) : ExprNode
  {
    public override Func<Row, MixedValue> BuildEvaluator()
    {
      var leftEvaluator = Left.BuildEvaluator();
      var rightEvaluator = Right.BuildEvaluator();
      if (Operator == BinaryOperatorType.And)
        return (Row r) => MixedValue.Bool(leftEvaluator(r).AsBool() && rightEvaluator(r).AsBool());
      else if (Operator == BinaryOperatorType.Or)
        return (Row r) => MixedValue.Bool(leftEvaluator(r).AsBool() || rightEvaluator(r).AsBool());
      else
        throw new NotImplementedException();
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      Left.PrettyPrint(sb);
      sb.Append(" " + Operator.ToString().ToUpper() + " ");
      Right.PrettyPrint(sb);
    }
  }


  public enum UnaryOperatorType { Not }

  public record UnaryOperatorNode(UnaryOperatorType Operator, ExprNode Expr) : ExprNode
  {
    public override Func<Row, MixedValue> BuildEvaluator()
    {
      var exprEvaluator = Expr.BuildEvaluator();
      return (Row r) => MixedValue.Bool(!exprEvaluator(r).AsBool());
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("NOT ");
      Expr.PrettyPrint(sb);
    }
  }
}
