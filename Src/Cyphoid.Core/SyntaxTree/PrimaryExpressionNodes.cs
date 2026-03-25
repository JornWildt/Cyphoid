using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.SyntaxTree
{
  public record PropertyAccessNode(VariableDefinition Variable, IReadOnlyList<string> Properties) 
    : ExprNode(null, ValueKindType.Variable)
  {
    public override RowEvaluator<TId> BuildEvaluator<TId>()
    {
      return (IRow<TId> r) =>
      {
        var v = r.Values[Variable.SlotIndex];
        if (v == null)
          return MixedValue.Null();
        var propertyName = Properties[0];
        var node = v.Value.AsGraphNode<TId>();
        if (node.Attributes.TryGetValue(propertyName, out var attributeValue))
          return MixedValue.FromObject(attributeValue);
        else
          return MixedValue.Null();
      };
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append(Variable.Name);
      foreach (var property in Properties)
      {
        sb.Append("." + property);
      }
    }
  }


  public record IdentifierNode(string Name) : AstNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append(Name);
    }
  }


  public record VariableExprNode(VariableDefinition Variable) 
    : ExprNode(Variable.Type, ValueKindType.Variable)
  {
    public override RowEvaluator<TId> BuildEvaluator<TId>()
    {
      return (IRow<TId> r) =>
      {
        var v = r.Values[Variable.SlotIndex];
        return v != null ? v.Value : MixedValue.Null();
      };
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append(Variable.Name);
    }
  }
}
