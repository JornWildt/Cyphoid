using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning
{
  public record NodeScanPlan<TId>(
    VariableDefinition Variable,
    string? Label,
    PropertyMapNode? PropertyMap) : PipelinePlan<TId> where TId : IEquatable<TId>
  {
    public override IOperator<TId> BuildExecutionPlan(IOperatorFactory<TId> factory)
    {
      // FIXME: No need to calculate this all the time
      var filter = BuildPropertyFilter(PropertyMap);
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
