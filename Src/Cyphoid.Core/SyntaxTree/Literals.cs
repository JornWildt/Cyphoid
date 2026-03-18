using System.Text;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.SyntaxTree
{
  public abstract record LiteralValueNode : ExprNode
  {
    public abstract MixedValue ToMixedValue();
  }

  public record BoolLiteralNode(bool Value) : LiteralValueNode
  {
    public override MixedValue ToMixedValue()
    {
      return MixedValue.Bool(Value);
    }

    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append(Value);
    }
  }


  public record IntLiteralNode(long Value) : LiteralValueNode
  {
    public override MixedValue ToMixedValue()
    {
      return MixedValue.Int(Value);
    }

    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append(Value);
    }
  }


  public record StringLiteralNode(string Value) : LiteralValueNode
  {
    public override MixedValue ToMixedValue()
    {
      return MixedValue.String(Value);
    }

    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("\"" + Value + "\"");
    }
  }
}
