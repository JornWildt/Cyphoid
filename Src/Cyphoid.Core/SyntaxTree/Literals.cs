using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.SyntaxTree
{
  public abstract record LiteralValueNode : ExprNode
  {
    public abstract MixedValue ToConstantValue();
  }


  public record BoolLiteralNode(bool Value) : LiteralValueNode
  {
    public override Func<Row, MixedValue> BuildEvaluator()
    {
      return (Row r) => ToConstantValue();
    }
    
    
    public override MixedValue ToConstantValue()
    {
      return MixedValue.Bool(Value);
    }

    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append(Value.ToString().ToLower());
    }
  }


  public record IntLiteralNode(long Value) : LiteralValueNode
  {
    public override Func<Row, MixedValue> BuildEvaluator()
    {
      return (Row r) => ToConstantValue();
    }


    public override MixedValue ToConstantValue()
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
    public override Func<Row, MixedValue> BuildEvaluator()
    {
      return (Row r) => ToConstantValue();
    }


    public override MixedValue ToConstantValue()
    {
      return MixedValue.String(Value);
    }

    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("\"" + Value + "\"");
    }
  }
}
