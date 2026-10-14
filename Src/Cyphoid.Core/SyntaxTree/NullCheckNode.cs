using System.Text;
using Cyphoid.Core.Exceptions;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.SyntaxTree
{
  public record NullCheckNode(ExprNode Expr, bool Negated)
    : ExprNode(Expr.ValueKind == ValueKindType.Aggregate
      ? throw new RuntimeException("Cannot use aggregate functions in expressions.")
      : Expr.ValueKind)
  {
    public override RowEvaluator BuildEvaluator()
    {
      var evaluator = Expr.BuildEvaluator();
      return (IRow row) => MixedValue.Bool(evaluator(row).IsNull() != Negated);
    }

    public override void PrettyPrint(StringBuilder sb)
    {
      Expr.PrettyPrint(sb);
      sb.Append(Negated ? " IS NOT NULL" : " IS NULL");
    }
  }
}
