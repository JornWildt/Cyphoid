using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning
{
  public record NodeScanPlan(
    VariableReference Variable,
    string? Label,
    PropertyMapNode? PropertyMap,
    RelationshipPatternNode? RelationshipDetail,
    IRowColumn[] MatchColumns) : PipelinePlan
  {
    public override IOperator BuildExecutionPlan(IOperatorFactory factory)
    {
      // FIXME: No need to calculate this all the time
      var filter = BuildPropertyFilter(PropertyMap);

      // Use optional immediate -[:label]-> if there is such a label, to filter the scanned nodes
      var targetRelationshipLabel = RelationshipDetail?.RelationshipDirection == RelationshipDirectionType.Right
        ? RelationshipDetail?.RelationshipDetail?.RelationshipType
        : null;

      return factory.BuildNodeScan(Variable.Definition, Label, filter, targetRelationshipLabel, MatchColumns);
    }
    
    
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("Nodescan (");
      if (!Variable.Definition.IsAnonymous)
        sb.Append(Variable.Definition.Name);
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
