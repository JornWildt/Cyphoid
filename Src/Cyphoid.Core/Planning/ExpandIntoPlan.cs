using System.Text;
using Cyphoid.Core.Exceptions;
using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning
{
  public record ExpandIntoPlan<TId>(
    PipelinePlan<TId> Input,
    VariableReference? Source,
    RelationshipPatternNode? Relationship,
    VariableReference Destination,
    string? DestinationLabel,
    PropertyMapNode? DestinationPropertyMap) : PipelinePlan<TId> where TId : IEquatable<TId>
  {
    public override IOperator<TId> BuildExecutionPlan(IOperatorFactory<TId> factory)
    {
      // FIXME: No need to calculate this all the time
      var destinationFilter = BuildPropertyFilter(DestinationPropertyMap);

#pragma warning disable CS8524 // unnamed enum values
      ExpandDirectionType? direction = Relationship?.RelationshipDirection switch
      {
        RelationshipDirectionType.Right => ExpandDirectionType.Outgoing,
        RelationshipDirectionType.Left => ExpandDirectionType.Incoming,
        RelationshipDirectionType.Unidirectional => ExpandDirectionType.Unidirectional
      };
#pragma warning restore CS8524

      return factory.BuildExpandInto(
        Input.BuildExecutionPlan(factory),
        Source?.Definition,
        direction,
        Relationship?.RelationshipDetail?.RelationshipType,
        Destination.Definition,
        DestinationLabel,
        destinationFilter);
    }

    public override void PrettyPrint(StringBuilder sb)
    {
      sb.AppendLine("Expand(into)");
      Input.PrettyPrint(sb);
    }
  }
}
