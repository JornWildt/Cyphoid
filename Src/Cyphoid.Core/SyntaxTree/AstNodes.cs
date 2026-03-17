using System.Text;
using Cyphoid.Core.Planning;

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

  
  public abstract record ExprNode : AstNode;

  
  public enum BinaryOperatorType { And, Or }

  public record BinaryOperatorNode(ExprNode Left, ExprNode Right, BinaryOperatorType Operator) : ExprNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      Left.PrettyPrint(sb);
      sb.Append(" " + Operator.ToString().ToUpper() + " ");
      Right.PrettyPrint(sb);
    }
  }


  public enum UnaryOperatorType { Not }

  public record UnaryOperatorNode(UnaryOperatorType Operator, ExprNode Expr) : ExprNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("NOT ");
      Expr.PrettyPrint(sb);
    }
  }


  public record IntLiteralNode(long Value) : ExprNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append(Value);
    }
  }


  public record StringLiteralNode(string Value) : ExprNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("\"" +  Value + "\"");
    }
  }

  
  public record IdentifierNode(string Name) : ExprNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append(Name);
    }
  }
}
