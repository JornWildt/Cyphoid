using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning
{
  public record ExpandPlan(
    PipelinePlan Input,
    VariableDefinition Source,
    RelationshipPatternNode Relationship,
    VariableDefinition Destination,
    string? DestinationLabel,
    PropertyMapNode? DestinationPropertyMap) : PipelinePlan
  {
    public override IOperator BuildExecutionPlan(IOperatorFactory factory)
    {
      // FIXME: No need to calculate this all the time
      var destinationFilter = BuildPropertyFilter(DestinationPropertyMap);

      var direction = Relationship.RelationshipDirection switch
      {
        RelationshipDirectionType.Right => ExpandDirectionType.Outgoing,
        RelationshipDirectionType.Left => ExpandDirectionType.Incoming,
        RelationshipDirectionType.Both => throw new NotImplementedException("Both directions not supported."),
        _ => throw new NotImplementedException()
      };

      return factory.BuildExpand(
        Input.BuildExecutionPlan(factory),
        Source,
        direction,
        Relationship.RelationshipDetail?.RelationshipType,
        Destination,
        DestinationLabel,
        destinationFilter);
    }

    public override void PrettyPrint(StringBuilder sb)
    {
      sb.AppendLine("Expand");
      Input.PrettyPrint(sb);
    }
  }
}
