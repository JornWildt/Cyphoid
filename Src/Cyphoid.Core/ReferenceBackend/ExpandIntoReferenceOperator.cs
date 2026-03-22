using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  // FIXME: Source not nullable (that would be cartesian product????)


  public record ExpandIntoReferenceOperator<TId>(
    IOperator<TId> Input,
    VariableDefinition? SourceVariable,
    ExpandDirectionType? Direction,
    string? RelationLabel,
    VariableDefinition DestinationVariable,
    string? DestinationLabel,
    PropertyFilter? DestinationPropertyFilter) : IOperator<TId> where TId : IEquatable<TId>
  {
    async IAsyncEnumerable<IRow<TId>> IOperator<TId>.ExecuteAsync(IQueryContext context)
    {
      await foreach (var row in Input.ExecuteAsync(context))
      {
        var sourceNode = row.Nodes[SourceVariable!.SlotIndex];
        if (sourceNode != null)
        {
          var matchingEdges = sourceNode.Edges
            .Where(e => RelationLabel == null || e.Key == RelationLabel);

          // FIXME: Direction!!!

          var destinationNode = row.Nodes[DestinationVariable!.SlotIndex];
          if (destinationNode != null)
          {
            // FIXME: Edge destination must be TId (not string)
            var isMatch = matchingEdges
              .Any(e => e.Value == destinationNode.Id.ToString());

            if (isMatch)
              yield return row;
          }
        }
      }
    }
  }
}
