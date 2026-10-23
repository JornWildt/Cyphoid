using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.SyntaxTree;

public sealed record ParameterNode(string Name) : ExprNode(ValueKindType.Variable)
{
  public override RowEvaluator BuildEvaluator() => throw new InvalidOperationException("Parameters must be bound before evaluation.");
  public override void PrettyPrint(StringBuilder sb) => sb.Append('$').Append(Name);
}

internal sealed record BoundValueNode(MixedValue Value) : LiteralValueNode(ValueKindType.Const)
{
  public override RowEvaluator BuildEvaluator() => _ => Value;
  public override MixedValue ToConstantValue() => Value;
  public override void PrettyPrint(StringBuilder sb) => sb.Append(Value);
}

internal static class ParameterBinding
{
  public static ExprNode Bind(this ExprNode expression, QueryParameters parameters)
  {
    ExprNode result = expression switch
    {
      ParameterNode p => new BoundValueNode(parameters.Require(p)),
      ImportedValueNode n => new BoundValueNode((parameters.CorrelatedRow
        ?? throw new InvalidOperationException("Missing correlated row.")).Values[n.Variable.SlotIndex] ?? MixedValue.Null()),
      CaseExpressionNode n => n with { Input = n.Input?.Bind(parameters),
        Alternatives = n.Alternatives.Select(a => a with { When = a.When.Bind(parameters), Then = a.Then.Bind(parameters) }).ToArray(),
        Else = n.Else?.Bind(parameters) },
      BinaryOperatorNode n => n with { Left = n.Left.Bind(parameters), Right = n.Right.Bind(parameters) },
      UnaryOperatorNode n => n with { Expr = n.Expr.Bind(parameters) },
      InOperatorNode n => n with { Expr = n.Expr.Bind(parameters), Items = n.Items.Select(e => e.Bind(parameters)).ToArray() },
      NullCheckNode n => n with { Expr = n.Expr.Bind(parameters) },
      SubExpression n => n with { Sub = n.Sub.Bind(parameters) },
      FunctionCallNode n => n with { Parameters = n.Parameters.Select(e => e.Bind(parameters)).ToArray() },
      MapLiteralNode n => n with { Entries = n.Entries.Select(e => e with { Value = e.Value.Bind(parameters) }).ToArray() },
      MapAccessNode n => n with { Target = n.Target.Bind(parameters), Key = n.Key.Bind(parameters) },
      MapProjectionNode n => n with { Target = n.Target.Bind(parameters), Entries = n.Entries.Select(e => e with { Value = e.Value?.Bind(parameters) }).ToArray() },
      _ => expression
    };
    result.SourceSpan ??= expression.SourceSpan;
    return result;
  }
  public static PropertyMapNode? Bind(this PropertyMapNode? map, QueryParameters parameters) => map == null ? null
    : map with { Properties = map.Properties.Select(p => p with { Value = p.Value.Bind(parameters) }).ToArray() };
  public static PatternNode Bind(this PatternNode pattern, QueryParameters parameters) => pattern with
  {
    Parts = pattern.Parts.Select(part => part with
    {
      PatternChain = part.PatternChain.Select(chain => chain with
      {
        NodePattern = chain.NodePattern with { PropertyMap = chain.NodePattern.PropertyMap.Bind(parameters) }
      }).ToArray()
    }).ToArray()
  };
}
