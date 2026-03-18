using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.SyntaxTree
{
  public enum BinaryOperatorType { And, Or, EQ, NEQ, LTE, GTE, LT, GT, CONTAINS, STARTS_WITH, ENDS_WITH }

  public record BinaryOperatorNode(ExprNode Left, ExprNode Right, BinaryOperatorType Operator) : ExprNode
  {
    private static readonly string[] OperatorSymbols = ["AND", "OR", "=", "<>", "<=", ">=", "<", ">", "CONTAINS", "STARTS WITH", "ENDS WITH"];
  

    public override Func<Row, MixedValue> BuildEvaluator()
    {
      var leftEvaluator = Left.BuildEvaluator();
      var rightEvaluator = Right.BuildEvaluator();
      return Operator switch
      {
        BinaryOperatorType.And => (Row r) => MixedValue.Bool(leftEvaluator(r).AsBool() && rightEvaluator(r).AsBool()),
        BinaryOperatorType.Or => (Row r) => MixedValue.Bool(leftEvaluator(r).AsBool() || rightEvaluator(r).AsBool()),
        BinaryOperatorType.EQ => (Row r) => MixedValue.Bool(leftEvaluator(r).Equals(rightEvaluator(r))),
        BinaryOperatorType.NEQ => (Row r) => MixedValue.Bool(!leftEvaluator(r).Equals(rightEvaluator(r))),
        //BinaryOperatorType.LTE => (Row r) => MixedValue.Bool(leftEvaluator(r).AsBool() || rightEvaluator(r).AsBool()),
        //BinaryOperatorType.GTE => (Row r) => MixedValue.Bool(leftEvaluator(r).AsBool() || rightEvaluator(r).AsBool()),
        //BinaryOperatorType.LT => (Row r) => MixedValue.Bool(leftEvaluator(r).AsBool() || rightEvaluator(r).AsBool()),
        //BinaryOperatorType.GT => (Row r) => MixedValue.Bool(leftEvaluator(r).AsBool() || rightEvaluator(r).AsBool()),
        _ => throw new NotImplementedException()
      };        
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      Left.PrettyPrint(sb);
      sb.Append(" " + OperatorSymbols[(int)Operator] + " ");
      Right.PrettyPrint(sb);
    }
  }


  public enum UnaryOperatorType { Not }

  public record UnaryOperatorNode(UnaryOperatorType Operator, ExprNode Expr) : ExprNode
  {
    public override Func<Row, MixedValue> BuildEvaluator()
    {
      var exprEvaluator = Expr.BuildEvaluator();
      return (Row r) => MixedValue.Bool(exprEvaluator(r).IsAnythingButTrue());
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("NOT ");
      Expr.PrettyPrint(sb);
    }
  }
}
