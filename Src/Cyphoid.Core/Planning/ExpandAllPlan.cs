using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;
using System.Text;

namespace Cyphoid.Core.Planning
{
  public record ExpandAllPlan(
    PipelinePlan Input,
    VariableReference Source,
    RelationshipPatternNode Relationship,
    VariableReference Destination,
    string? DestinationLabel,
    PropertyMapNode? DestinationPropertyMap,
    RowSchema MatchColumns) : PipelinePlan
  {
    public override Func<QueryParameters, IOperator> Compile(IOperatorFactory factory)
    {
      var input = Input.Compile(factory);

#pragma warning disable CS8524 // unnamed enum values
      var direction = Relationship.RelationshipDirection switch
      {
        RelationshipDirectionType.Right => ExpandDirectionType.Outgoing,
        RelationshipDirectionType.Left => ExpandDirectionType.Incoming,
        RelationshipDirectionType.Unidirectional => ExpandDirectionType.Unidirectional,
      };
#pragma warning restore CS8524

      return parameters => factory.BuildExpandAll(
        input(parameters),
        Source.Definition,
        direction,
        Relationship.RelationshipDetail?.RelationshipType,
        Destination.Definition,
        DestinationLabel,
        BuildPropertyFilter(DestinationPropertyMap, parameters),
        MatchColumns);
    }

    public override void PrettyPrint(StringBuilder sb)
    {
      sb.AppendLine("Expand(all)");
      Input.PrettyPrint(sb);
    }
  }
}
