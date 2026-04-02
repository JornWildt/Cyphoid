using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;
using System.Text;

namespace Cyphoid.Core.SyntaxTree
{
  public abstract record LiteralValueNode(
    ValueKindType ValueKind) : ExprNode(ValueKind)
  {
    public abstract MixedValue ToConstantValue();
  }


  public record NullLiteralNode() 
    : LiteralValueNode(ValueKindType.Const)
  {
    public override RowEvaluator<TId> BuildEvaluator<TId>()
    {
      return (IRow<TId> r) => MixedValue.Null();
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


  public record BoolLiteralNode(bool Value)
    : LiteralValueNode(ValueKindType.Const)
  {
    public override RowEvaluator<TId> BuildEvaluator<TId>()
    {
      return (IRow<TId> r) => ToConstantValue();
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


  public record IntLiteralNode(long Value)
    : LiteralValueNode(ValueKindType.Const)
  {
    public override RowEvaluator<TId> BuildEvaluator<TId>()
    {
      return (IRow<TId> r) => ToConstantValue();
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


  public record StringLiteralNode(string Value)
    : LiteralValueNode(ValueKindType.Const)
  {
    public override RowEvaluator<TId> BuildEvaluator<TId>()
    {
      return (IRow<TId> r) => ToConstantValue();
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
