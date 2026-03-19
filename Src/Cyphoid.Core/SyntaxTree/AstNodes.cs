using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;
using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Core.SyntaxTree
{
  public abstract record AstNode
  {
    public virtual string PrettyPrint()
    {
      StringBuilder sb = new StringBuilder();
      PrettyPrint(sb);
      return sb.ToString();
    }

    public virtual void PrettyPrint(StringBuilder sb) { sb.Append($"-not-implemented({GetType().Name})-"); }
  }


  public record VariableNode(string Name) : AstNode;

  public record LabelNode(string Label) : AstNode;


  public abstract record ExprNode : AstNode
  {
    public abstract RowEvaluator BuildEvaluator();
  }


  public record VariableExprNode(VariableDefinition Variable) : ExprNode
  {
    public override RowEvaluator BuildEvaluator()
    {
      return (IRow r) =>
      {
        var node = r.Nodes[Variable.SlotIndex];
        return MixedValue.GraphNode(node);
      };
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append(Variable.Name);
    }
  }


  public record PropertyAccessNode(VariableDefinition Variable, IReadOnlyList<string> Properties) : ExprNode
  {
    public override RowEvaluator BuildEvaluator()
    {
      return (IRow r) =>
      {
        var node = r.Nodes[Variable.SlotIndex];
        if (node == null)
          return MixedValue.Null();
        var propertyName = Properties[0];
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
}
