using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning;

/// <summary>Conservative, clause-local extraction of indexable WHERE comparisons.</summary>
public sealed record PropertyConstraintNode(string Identifier, ExprNode Value, PropertyComparison Comparison);

internal static class MatchPredicatePushdown
{
  public static PipelinePlan Apply(PipelinePlan plan, PipelinePlan? input, ExprNode? predicate)
  {
    if (predicate == null) return plan;
    var constraints = new List<(VariableDefinition Variable, PropertyConstraintNode Entry)>();
    foreach (var term in Conjuncts(predicate))
    {
      // Only move a leading sequence of deterministic comparisons. Moving
      // a later comparison before rand(), EXISTS, or a potentially failing expression
      // can change evaluation counts or suppress an error. OR is never split.
      if (Unwrap(term) is not BinaryOperatorNode equality) break;
      var comparison = equality.Operator switch
      {
        BinaryOperatorType.EQ => PropertyComparison.Equal,
        BinaryOperatorType.LT => PropertyComparison.LessThan,
        BinaryOperatorType.LTE => PropertyComparison.LessThanOrEqual,
        BinaryOperatorType.GT => PropertyComparison.GreaterThan,
        BinaryOperatorType.GTE => PropertyComparison.GreaterThanOrEqual,
        _ => (PropertyComparison?)null
      };
      if (comparison == null) break;
      var left = Unwrap(equality.Left);
      var right = Unwrap(equality.Right);
      if (left is LiteralValueNode or ParameterNode)
      {
        (left, right) = (right, left);
        comparison = comparison switch
        {
          PropertyComparison.LessThan => PropertyComparison.GreaterThan,
          PropertyComparison.LessThanOrEqual => PropertyComparison.GreaterThanOrEqual,
          PropertyComparison.GreaterThan => PropertyComparison.LessThan,
          PropertyComparison.GreaterThanOrEqual => PropertyComparison.LessThanOrEqual,
          _ => comparison
        };
      }
      if (left is not PropertyAccessNode { Properties.Count: 1 } property ||
          right is not (LiteralValueNode or ParameterNode)) break;
      // Keep map-valued constraints residual. Unknown results must still allow
      // later operands to run, including any source-located failures.
      if (right is LiteralValueNode literal && literal.ToConstantValue().Type == Cyphoid.Core.Expressions.MixedValue.ValueType.Map)
        break;
      constraints.Add((property.Variable, new PropertyConstraintNode(property.Properties[0], right, comparison.Value)));
      // A range may fail on a different property type. Do not push later terms
      // ahead of its residual evaluation: they could hide that failure. The
      // candidate matcher retains incompatible values instead of throwing.
      if (comparison != PropertyComparison.Equal) break;
      // A parameter may be a map at execution time, and comparing a map property
      // with null also produces unknown. Neither may have later conditions moved
      // ahead of it; IsMatch retains unknown candidates for the residual evaluator.
      if (right is ParameterNode || right is LiteralValueNode value && value.ToConstantValue().IsNull()) break;
    }

    IReadOnlyList<PropertyConstraintNode> For(VariableDefinition variable) => constraints
      .Where(c => c.Variable == variable).Select(c => c.Entry).ToArray();

    PipelinePlan Visit(PipelinePlan stage)
    {
      // MATCH schemas reserve slots for the entire pattern. A reserved slot is
      // not evidence that its variable is bound! Attach a constraint only to the
      // scan/expansion that actually supplies that node. Never cross the incoming
      // clause boundary (WITH, LIMIT, procedures, and OPTIONAL have other rules).
      if (ReferenceEquals(stage, input)) return stage;
      return stage switch
      {
        NodeScanPlan scan => scan with { WhereConstraints = For(scan.Variable.Definition) },
        ExpandAllPlan expand => expand with { Input = Visit(expand.Input),
          WhereConstraints = For(expand.Destination.Definition) },
        ExpandIntoPlan expand => expand with { Input = Visit(expand.Input),
          WhereConstraints = For(expand.Destination.Definition) },
        CartesianProductPlan product => product with { Left = Visit(product.Left), Right = Visit(product.Right) },
        _ => stage
      };
    }

    // Keep the original filter as the semantic authority. Constraints can retain
    // unknown candidates; the residual preserves source-located evaluation and
    // the existing boolean/error behavior for those rows.
    return Visit(plan);
  }

  private static ExprNode Unwrap(ExprNode node) => node is SubExpression sub ? Unwrap(sub.Sub) : node;

  private static IEnumerable<ExprNode> Conjuncts(ExprNode node)
  {
    if (Unwrap(node) is BinaryOperatorNode { Operator: BinaryOperatorType.And } and)
    {
      foreach (var term in Conjuncts(and.Left)) yield return term;
      foreach (var term in Conjuncts(and.Right)) yield return term;
    }
    else yield return node;
  }
}

