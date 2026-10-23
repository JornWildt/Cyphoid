using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.SyntaxTree;

public sealed record CaseAlternativeNode(ExprNode When, ExprNode Then);

public sealed record CaseExpressionNode(ExprNode? Input,
  IReadOnlyList<CaseAlternativeNode> Alternatives, ExprNode? Else)
  : ExprNode(Input?.ValueKind == ValueKindType.Variable ||
      Alternatives.Any(a => a.When.ValueKind == ValueKindType.Variable || a.Then.ValueKind == ValueKindType.Variable) ||
      Else?.ValueKind == ValueKindType.Variable ? ValueKindType.Variable : ValueKindType.Const)
{
  public override RowEvaluator BuildEvaluator()
  {
    var input = Input?.CompileEvaluator();
    var alternatives = Alternatives.Select(a => (When: a.When.CompileEvaluator(), Then: a.Then.CompileEvaluator())).ToArray();
    var fallback = Else?.CompileEvaluator();
    return row =>
    {
      // Evaluate the selector once, and only the chosen result. Unselected branches
      // must not execute functions, trigger errors, or run EXISTS subqueries.
      var value = input?.Invoke(row) ?? MixedValue.Null();
      foreach (var alternative in alternatives)
      {
        var candidate = alternative.When(row);
        var matches = input == null ? MixedValue.QueryBoolean(candidate) == true
          : !MixedValue.QueryEquals(value, candidate).IsAnythingButTrue();
        if (matches) return alternative.Then(row);
      }
      return fallback?.Invoke(row) ?? MixedValue.Null();
    };
  }

  public override void PrettyPrint(StringBuilder sb)
  {
    sb.Append("CASE");
    if (Input != null) { sb.Append(' '); Input.PrettyPrint(sb); }
    foreach (var alternative in Alternatives)
    {
      sb.Append(" WHEN "); alternative.When.PrettyPrint(sb);
      sb.Append(" THEN "); alternative.Then.PrettyPrint(sb);
    }
    if (Else != null) { sb.Append(" ELSE "); Else.PrettyPrint(sb); }
    sb.Append(" END");
  }
}
