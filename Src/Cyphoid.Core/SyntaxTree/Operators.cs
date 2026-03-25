using System.Diagnostics;
using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;
using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Core.SyntaxTree
{
  public enum BinaryOperatorType { And, Or, EQ, NEQ, LTE, GTE, LT, GT, CONTAINS, STARTS_WITH, ENDS_WITH }

  public record BinaryOperatorNode(
    ExprNode Left, 
    ExprNode Right, 
    BinaryOperatorType Operator) 
    : ExprNode(EvaluateType(Left, Right, Operator), EvaluateKind(Left, Right, Operator))
  {
    private static readonly string[] OperatorSymbols = ["AND", "OR", "=", "<>", "<=", ">=", "<", ">", "CONTAINS", "STARTS WITH", "ENDS WITH"];

    
    private static MixedValue.ValueType EvaluateType(
      ExprNode left, 
      ExprNode right, 
      BinaryOperatorType op)
    {
      // So for we only have bool operators
      return MixedValue.ValueType.Bool;
    }


    private static ValueKindType EvaluateKind(
      ExprNode left,
      ExprNode right,
      BinaryOperatorType op)
    {
      if (!Enum.IsDefined(left.ValueKind))
        throw new ArgumentOutOfRangeException(nameof(left.ValueKind), left.ValueKind, "Invalid enum value.");

#pragma warning disable CS8524 // unnamed enum values
      // So for we only have bool operators
      if (left.ValueKind == ValueKindType.Const)
      {
        return right.ValueKind switch
        {
          ValueKindType.Const => ValueKindType.Const,
          ValueKindType.Variable => ValueKindType.Variable,
          ValueKindType.Aggregate => throw new NotImplementedException()
        };
      }
      else if (left.ValueKind == ValueKindType.Variable)
      {
        return right.ValueKind switch
        {
          ValueKindType.Const => ValueKindType.Variable,
          ValueKindType.Variable => ValueKindType.Variable,
          ValueKindType.Aggregate => throw new NotImplementedException()
        };
      }
      else
      {
        throw new NotImplementedException();
      }
#pragma warning restore CS8524
    }


    public override RowEvaluator<TId> BuildEvaluator<TId>()
    {
      var leftEvaluator = Left.BuildEvaluator<TId>();
      var rightEvaluator = Right.BuildEvaluator<TId>();
      return Operator switch
      {
        BinaryOperatorType.And => (IRow<TId> r) => MixedValue.Bool(leftEvaluator(r).AsBool() && rightEvaluator(r).AsBool()),
        BinaryOperatorType.Or => (IRow<TId> r) => MixedValue.Bool(leftEvaluator(r).AsBool() || rightEvaluator(r).AsBool()),
        BinaryOperatorType.EQ => (IRow<TId> r) => MixedValue.Bool(leftEvaluator(r).Equals(rightEvaluator(r))),
        BinaryOperatorType.NEQ => (IRow<TId> r) => MixedValue.Bool(!leftEvaluator(r).Equals(rightEvaluator(r))),
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

  public record UnaryOperatorNode(UnaryOperatorType Operator, ExprNode Expr) 
    : ExprNode(MixedValue.ValueType.Bool, Expr.ValueKind)
  {
    public override RowEvaluator<TId> BuildEvaluator<TId>()
    {
      var exprEvaluator = Expr.BuildEvaluator<TId>();
      return (IRow<TId> r) => MixedValue.Bool(exprEvaluator(r).IsAnythingButTrue());
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("NOT ");
      Expr.PrettyPrint(sb);
    }
  }


  public record InOperatorNode(ExprNode Expr, IReadOnlyList<ExprNode> Items)
    : ExprNode(MixedValue.ValueType.Bool, ValueKindType.Variable)
  {
    public override RowEvaluator<TId> BuildEvaluator<TId>()
    {
      var exprEvaluator = Expr.BuildEvaluator<TId>();
      var itemEvaluators = Items.Select(i => i.BuildEvaluator<TId>());
      return (IRow<TId> r) =>
      {
        var exprValue = exprEvaluator(r);
        var any = itemEvaluators.Any(e => e(r).Equals(exprValue));
        return MixedValue.Bool(any);
      };
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      Expr.PrettyPrint(sb);
      sb.Append(" IN [");
      bool first = true;
      foreach (var item in Items)
      {
        if (!first)
          sb.Append(", ");
        item.PrettyPrint(sb);
        first = false;
      }
      sb.Append("]");
    }
  }
}
