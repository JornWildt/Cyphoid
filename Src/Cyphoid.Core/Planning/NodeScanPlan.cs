using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning
{
  public record NodeScanPlan(
    NodeVariable? Variable,
    string? Label,
    PropertyMapNode? PropertyMap) : LogicalPlan
  {
    public override IOperator BuildExecutionPlan(IOperatorFactory factory)
    {
      return factory.BuildNodeScan();
    }
    
    
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("Nodescan (");
      if (Variable != null)
        sb.Append(Variable.Name);
      if (Label != null)
        sb = sb.Append(":" + Label);
      if (PropertyMap != null)
      {
        sb.Append(" ");
        PropertyMap.PrettyPrint(sb);
      }
      sb.AppendLine(")");
    }
  }
}
