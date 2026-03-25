using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

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


  public enum ValueKindType { Const, Variable, Aggregate }

  public abstract record ExprNode(
    MixedValue.ValueType? Type,
    ValueKindType ValueKind) : AstNode
  {
    public abstract RowEvaluator<TId> BuildEvaluator<TId>() where TId : IEquatable<TId>;
  }
}
