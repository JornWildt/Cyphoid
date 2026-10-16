using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.SyntaxTree
{
  public record PropertyAccessNode(VariableDefinition Variable, IReadOnlyList<string> Properties) 
    : ExprNode(ValueKindType.Variable)
  {
    public override RowEvaluator BuildEvaluator()
    {
      return (IRow r) =>
      {
        var value = r.Values[Variable.SlotIndex] ?? MixedValue.Null();
        foreach (var property in Properties)
          value = MapAccessNode.Read(value, MixedValue.String(property));
        return value;
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
    : ExprNode(ValueKindType.Variable)
  {
    public override RowEvaluator BuildEvaluator()
    {
      return (IRow r) =>
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
