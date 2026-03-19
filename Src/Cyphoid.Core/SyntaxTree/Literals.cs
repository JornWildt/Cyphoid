using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;
using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Core.SyntaxTree
{
  public abstract record LiteralValueNode : ExprNode
  {
    public abstract MixedValue ToConstantValue();
  }


  public record NullLiteralNode() : LiteralValueNode
  {
    public override RowEvaluator BuildEvaluator()
    {
      return (IRow r) => MixedValue.Null();
    }


    public override MixedValue ToConstantValue()
    {
      return MixedValue.Null();
    }

    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("null");
    }
  }


  public record BoolLiteralNode(bool Value) : LiteralValueNode
  {
    public override RowEvaluator BuildEvaluator()
    {
      return (IRow r) => ToConstantValue();
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
    public override RowEvaluator BuildEvaluator()
    {
      return (IRow r) => ToConstantValue();
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
    public override RowEvaluator BuildEvaluator()
    {
      return (IRow r) => ToConstantValue();
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
