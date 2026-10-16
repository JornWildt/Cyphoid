using System.Text;
using Cyphoid.Core.Exceptions;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.SyntaxTree
{
  public enum BinaryOperatorType { 
    And, Or, EQ, NEQ, LTE, GTE, LT, GT, CONTAINS, STARTS_WITH, ENDS_WITH, 
    Add, Sub, Mult, Div, Mod }

  public record BinaryOperatorNode(
    ExprNode Left,
    ExprNode Right,
    BinaryOperatorType Operator)
    : ExprNode(EvaluateKind(Left, Right, Operator))
  {
    private static readonly string[] OperatorSymbols = [
      "AND", "OR", "=", "<>", "<=", ">=", "<", ">", "CONTAINS", "STARTS WITH", "ENDS WITH",
      "+", "-", "*", "/", "%"];


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
          ValueKindType.Aggregate => throw new ParseException("Cannot use aggregate functions in expressions.", CypherErrorCode.InvalidAggregateUsage)
        };
      }
      else if (left.ValueKind == ValueKindType.Variable)
      {
        return right.ValueKind switch
        {
          ValueKindType.Const => ValueKindType.Variable,
          ValueKindType.Variable => ValueKindType.Variable,
          ValueKindType.Aggregate => throw new ParseException("Cannot use aggregate functions in expressions.", CypherErrorCode.InvalidAggregateUsage)
        };
      }
      else
      {
        throw new ParseException("Cannot use aggregate functions in expressions.", CypherErrorCode.InvalidAggregateUsage);
      }
#pragma warning restore CS8524
    }


    public override RowEvaluator BuildEvaluator()
    {
      var leftEvaluator = Left.CompileEvaluator();
      var rightEvaluator = Right.CompileEvaluator();

#pragma warning disable CS8524 // unnamed enum values
      return Operator switch
      {
        BinaryOperatorType.And => (IRow r) => MixedValue.Bool(leftEvaluator(r).AsBool() && rightEvaluator(r).AsBool()),
        BinaryOperatorType.Or => (IRow r) => MixedValue.Bool(leftEvaluator(r).AsBool() || rightEvaluator(r).AsBool()),
        BinaryOperatorType.EQ => (IRow r) => MixedValue.QueryEquals(leftEvaluator(r), rightEvaluator(r)),
        BinaryOperatorType.NEQ => (IRow r) =>
        {
          var equal = MixedValue.QueryEquals(leftEvaluator(r), rightEvaluator(r));
          return equal.IsNull() ? equal : MixedValue.Bool(!equal.AsBool());
        },
        BinaryOperatorType.LTE => (IRow r) => leftEvaluator(r) <= rightEvaluator(r),
        BinaryOperatorType.LT => (IRow r) => leftEvaluator(r) < rightEvaluator(r),
        BinaryOperatorType.GTE => (IRow r) => leftEvaluator(r) >= rightEvaluator(r),
        BinaryOperatorType.GT => (IRow r) => leftEvaluator(r) > rightEvaluator(r),
        BinaryOperatorType.Add => (IRow r) => leftEvaluator(r) + rightEvaluator(r),
        BinaryOperatorType.Sub => (IRow r) => leftEvaluator(r) - rightEvaluator(r),
        BinaryOperatorType.Mult => (IRow r) => leftEvaluator(r) * rightEvaluator(r),
        BinaryOperatorType.Div => (IRow r) => leftEvaluator(r) / rightEvaluator(r),
        BinaryOperatorType.Mod => (IRow r) => leftEvaluator(r) % rightEvaluator(r),
        BinaryOperatorType.CONTAINS => BuildStringEvaluator(leftEvaluator, rightEvaluator, (left, right) => left.Contains(right, StringComparison.Ordinal)),
        BinaryOperatorType.STARTS_WITH => BuildStringEvaluator(leftEvaluator, rightEvaluator, (left, right) => left.StartsWith(right, StringComparison.Ordinal)),
        BinaryOperatorType.ENDS_WITH => BuildStringEvaluator(leftEvaluator, rightEvaluator, (left, right) => left.EndsWith(right, StringComparison.Ordinal))
      };
#pragma warning restore CS8524
    }


    private static RowEvaluator BuildStringEvaluator(
      RowEvaluator leftEvaluator, RowEvaluator rightEvaluator, Func<string, string, bool> matches)
    {
      return (IRow row) =>
      {
        var left = leftEvaluator(row);
        var right = rightEvaluator(row);
        return left.TryGetString(out var text) && right.TryGetString(out var pattern)
          ? MixedValue.Bool(matches(text, pattern))
          : MixedValue.Null();
      };
    }

    public override void PrettyPrint(StringBuilder sb)
    {
      Left.PrettyPrint(sb);
      sb.Append(" " + OperatorSymbols[(int)Operator] + " ");
      Right.PrettyPrint(sb);
    }
  }


  public enum UnaryOperatorType { Not, Plus, Minus }

  public record UnaryOperatorNode(UnaryOperatorType Operator, ExprNode Expr)
    : ExprNode(Expr.ValueKind)
  {
    private static readonly string[] OperatorSymbols = ["NOT ", "+", "-"];

    public override RowEvaluator BuildEvaluator()
    {
      var exprEvaluator = Expr.CompileEvaluator();

#pragma warning disable CS8524 // unnamed enum values
      return Operator switch
      {
        UnaryOperatorType.Not => (IRow r) => MixedValue.Bool(exprEvaluator(r).IsAnythingButTrue()),
        UnaryOperatorType.Plus => (IRow r) => +exprEvaluator(r),
        UnaryOperatorType.Minus => (IRow r) => -exprEvaluator(r)
      };
    }
#pragma warning restore CS8524


    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append(OperatorSymbols[(int)Operator]);
      Expr.PrettyPrint(sb);
    }
  }


  public record InOperatorNode(ExprNode Expr, IReadOnlyList<ExprNode> Items)
    : ExprNode(ValueKindType.Variable)
  {
    public override RowEvaluator BuildEvaluator()
    {
      var exprEvaluator = Expr.CompileEvaluator();
      var itemEvaluators = Items.Select(i => i.CompileEvaluator());
      return (IRow r) =>
      {
        var exprValue = exprEvaluator(r);
        var unknown = false;
        foreach (var evaluate in itemEvaluators)
        {
          var equal = MixedValue.QueryEquals(exprValue, evaluate(r));
          if (equal.IsNull()) unknown = true;
          else if (equal.AsBool()) return MixedValue.Bool(true);
        }
        return unknown ? MixedValue.Null() : MixedValue.Bool(false);
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
