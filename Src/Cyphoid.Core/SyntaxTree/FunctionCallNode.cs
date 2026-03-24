using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.SyntaxTree
{
  public record FunctionCallNode(
    string FunctionName,
    IReadOnlyList<ExprNode> Parameters) : ExprNode
  {
    public override RowEvaluator<TId> BuildEvaluator<TId>()
    {
      if (FunctionName == "CountAll")
      {
        return (IRow<TId> row) => MixedValue.Int(-1);
      }

      throw new NotImplementedException($"Unknown function '{FunctionName}'.");
    }

    public override void PrettyPrint(StringBuilder sb)
    {
      if (FunctionName == "CountAll")
      {
        sb.Append("COUNT(*)");
      }
      else
      {
        sb.Append(FunctionName);
        sb.Append("(");
        bool first = true;
        foreach (var p in Parameters)
        {
          if (!first)
            sb.Append(", ");
          p.PrettyPrint(sb);
          first = false;
        }
        sb.Append(")");
      }
    }
  }
}
