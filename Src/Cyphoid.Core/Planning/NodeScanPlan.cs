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
    RowSchema MatchColumns) : PipelinePlan
  {
    /// <summary>Safe WHERE comparisons on this operator's candidate node.</summary>
    public IReadOnlyList<PropertyConstraintNode> WhereConstraints { get; init; } = [];

    public override Func<QueryParameters, IOperator> Compile(IOperatorFactory factory)
    {
      // Use optional immediate -[:type]-> if there is such a relationship type, to filter the scanned nodes
      var targetRelationshipType = RelationshipDetail?.RelationshipDirection == RelationshipDirectionType.Right
        ? RelationshipDetail?.RelationshipDetail?.RelationshipType
        : null;

      return parameters => factory.BuildNodeScan(Variable.Definition, Label,
        BuildPropertyFilter(PropertyMap, parameters, WhereConstraints), targetRelationshipType, MatchColumns);
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
