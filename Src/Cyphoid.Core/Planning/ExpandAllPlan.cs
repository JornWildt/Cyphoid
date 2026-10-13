using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;
using System.Text;

namespace Cyphoid.Core.Planning
{
  public record ExpandAllPlan(
    PipelinePlan Input,
    bool Optional,
    VariableReference Source,
    RelationshipPatternNode Relationship,
    VariableReference Destination,
    string? DestinationLabel,
    PropertyMapNode? DestinationPropertyMap,
    IRowColumn[] MatchColumns) : PipelinePlan
  {
    public override IOperator BuildExecutionPlan(IOperatorFactory factory)
    {
      var destinationFilter = BuildPropertyFilter(DestinationPropertyMap);

#pragma warning disable CS8524 // unnamed enum values
      var direction = Relationship.RelationshipDirection switch
      {
        RelationshipDirectionType.Right => ExpandDirectionType.Outgoing,
        RelationshipDirectionType.Left => ExpandDirectionType.Incoming,
        RelationshipDirectionType.Unidirectional => ExpandDirectionType.Unidirectional,
      };
#pragma warning restore CS8524

      return factory.BuildExpandAll(
        Input.BuildExecutionPlan(factory),
        Optional,
        Source.Definition,
        direction,
        Relationship.RelationshipDetail?.RelationshipType,
        Destination.Definition,
        DestinationLabel,
        destinationFilter,
        MatchColumns);
    }

    public override void PrettyPrint(StringBuilder sb)
    {
      sb.AppendLine("Expand(all)");
      Input.PrettyPrint(sb);
    }
  }
}
