using System.Text;
using Cyphoid.Core.Execution;

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
    public abstract RowEvaluator<TId> BuildEvaluator<TId>() where TId : IEquatable<TId>;
  }
}
