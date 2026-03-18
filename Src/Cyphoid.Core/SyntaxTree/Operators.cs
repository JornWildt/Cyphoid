using System.Text;

namespace Cyphoid.Core.SyntaxTree
{
  public enum BinaryOperatorType { And, Or }

  public record BinaryOperatorNode(ExprNode Left, ExprNode Right, BinaryOperatorType Operator) : ExprNode
  {
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
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("NOT ");
      Expr.PrettyPrint(sb);
    }
  }
}
