using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;
using System.Text;

namespace Cyphoid.Core.SyntaxTree
{
  public abstract record LiteralValueNode(
    MixedValue.ValueType? Type,
    ValueKindType ValueKind) : ExprNode(Type, ValueKind)
  {
    public abstract MixedValue ToConstantValue();
  }


  public record NullLiteralNode() 
    : LiteralValueNode(MixedValue.ValueType.Null, ValueKindType.Const)
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
    : LiteralValueNode(MixedValue.ValueType.Bool, ValueKindType.Const)
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
    : LiteralValueNode(MixedValue.ValueType.Int, ValueKindType.Const)
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
    : LiteralValueNode(MixedValue.ValueType.String, ValueKindType.Const)
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
