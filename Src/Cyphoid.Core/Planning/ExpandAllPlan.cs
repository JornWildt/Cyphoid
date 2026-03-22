using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning
{
  public record ExpandAllPlan<TId>(
    PipelinePlan<TId> Input,
    VariableReference Source,
    RelationshipPatternNode Relationship,
    VariableReference Destination,
    string? DestinationLabel,
    PropertyMapNode? DestinationPropertyMap) : PipelinePlan<TId> where TId : IEquatable<TId>
  {
    public override IOperator<TId> BuildExecutionPlan(IOperatorFactory<TId> factory)
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

      // FIXME: Check bound/unbound variable

      return factory.BuildExpandAll(
        Input.BuildExecutionPlan(factory),
        Source.Definition,
        direction,
        Relationship.RelationshipDetail?.RelationshipType,
        Destination.Definition,
        DestinationLabel,
        destinationFilter);
    }

    public override void PrettyPrint(StringBuilder sb)
    {
      sb.AppendLine("Expand(all)");
      Input.PrettyPrint(sb);
    }
  }
}
