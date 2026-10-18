using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;
using System.Text;

namespace Cyphoid.Core.Planning
{
  public record ExpandIntoPlan(
    PipelinePlan Input,
    VariableReference? Source,
    RelationshipPatternNode? Relationship,
    VariableReference Destination,
    string? DestinationLabel,
    PropertyMapNode? DestinationPropertyMap) : PipelinePlan
  {
    /// <summary>Safe WHERE comparisons on this operator's candidate node.</summary>
    public IReadOnlyList<PropertyConstraintNode> WhereConstraints { get; init; } = [];

    public override Func<QueryParameters, IOperator> Compile(IOperatorFactory factory)
    {
      var input = Input.Compile(factory);

#pragma warning disable CS8524 // unnamed enum values
      ExpandDirectionType? direction = Relationship?.RelationshipDirection switch
      {
        RelationshipDirectionType.Right => ExpandDirectionType.Outgoing,
        RelationshipDirectionType.Left => ExpandDirectionType.Incoming,
        RelationshipDirectionType.Unidirectional => ExpandDirectionType.Unidirectional,
        null => null
      };
#pragma warning restore CS8524

      return parameters => factory.BuildExpandInto(
        input(parameters),
        Source?.Definition,
        direction,
        Relationship?.RelationshipDetail?.RelationshipType,
        Destination.Definition,
        DestinationLabel,
        BuildPropertyFilter(DestinationPropertyMap, parameters, WhereConstraints));
    }

    public override void PrettyPrint(StringBuilder sb)
    {
      sb.AppendLine("Expand(into)");
      Input.PrettyPrint(sb);
    }
  }
}
