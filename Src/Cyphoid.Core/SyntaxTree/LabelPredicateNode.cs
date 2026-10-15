using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.SyntaxTree
{
  public record LabelPredicateNode(VariableDefinition Variable, string Label) : ExprNode(ValueKindType.Variable)
  {
    public override RowEvaluator BuildEvaluator()
    {
      var evaluator = new VariableExprNode(Variable).BuildEvaluator();
      return (IRow row) =>
      {
        var value = evaluator(row);
        if (value.IsNull())
          return MixedValue.Null();
        return MixedValue.Bool(string.Equals(value.AsGraphNode().Type, Label, StringComparison.Ordinal));
      };
    }

    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append(Variable.Name);
      sb.Append(':');
      sb.Append(Label);
    }
  }
}
