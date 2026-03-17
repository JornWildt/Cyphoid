using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning
{
  public record ProjectPlan(
    LogicalPlan Input,
    IReadOnlyList<ReturnItemNode> Items) : LogicalPlan
  {
    public override IOperator BuildExecutionPlan(IOperatorFactory factory)
    {
      return factory.BuildProjection(Input.BuildExecutionPlan(factory));
    }

    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("Project: ");
      bool first = true;
      foreach (var i in Items)
      {
        if (!first)
          sb.Append(", ");
        i.Expr.PrettyPrint(sb);
        if (i.Identifier != null)
          sb.Append(" AS " + i.Identifier);
      }
      sb.AppendLine();
      Input.PrettyPrint(sb);
    }
  }
}
