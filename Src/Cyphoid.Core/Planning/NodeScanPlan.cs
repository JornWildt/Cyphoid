using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning
{
  public record NodeScanPlan(
    VariableDefinition Variable,
    string? Label,
    PropertyMapNode? PropertyMap) : LogicalPlan
  {
    public override IOperator BuildExecutionPlan(IOperatorFactory factory)
    {
      var conditions = PropertyMap?
        .Properties?
        .Select(p => new PropertyFilterCondition(p.Identifier, p.Value.ToMixedValue()))
        .ToList()
        .AsReadOnly();

      var filter = conditions != null ? new PropertyFilter(conditions) : null;
      return factory.BuildNodeScan(Variable, Label, filter);
    }
    
    
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("Nodescan (");
      if (!Variable.IsAnonymous)
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
