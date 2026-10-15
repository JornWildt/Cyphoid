using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  // FIXME: Source not nullable (that would be cartesian product????)


  public record ExpandIntoReferenceOperator(
    IOperator Input,
    VariableDefinition? SourceVariable,
    ExpandDirectionType? Direction,
    string? RelationLabel,
    VariableDefinition DestinationVariable,
    string? DestinationLabel,
    PropertyFilter? DestinationPropertyFilter) : IOperator
  {
    async IAsyncEnumerable<IRow> IOperator.ExecuteAsync(IQueryContext context)
    {
      await foreach (var row in Input.ExecuteAsync(context))
      {
        var destinationVariable = row.Values[DestinationVariable.SlotIndex];
        if (destinationVariable != null)
        {
          var destinationNode = destinationVariable.Value.AsGraphNode();

          bool isMatch = false;

          if (SourceVariable != null)
          {
            var sourceVariable = row.Values[SourceVariable.SlotIndex];
            if (sourceVariable != null)
            {
              var sourceNode = sourceVariable.Value.AsGraphNode();
              if (Direction == ExpandDirectionType.Outgoing)
              {
                var matchingEdges = sourceNode.Edges
                  .Where(e => RelationLabel == null || e.Type == RelationLabel);

                if (matchingEdges.Any(e => e.RefersTo(destinationNode)))
                  isMatch = true;
              }
              else
              {
                var matchingEdges = destinationNode.Edges
                  .Where(e => RelationLabel == null || e.Type == RelationLabel);

                // FIXME: Edge destination must be TId (not string)
                if (matchingEdges.Any(e => e.RefersTo(sourceNode)))
                  isMatch = true;
              }
            }
          }
          else
          {
            isMatch = true;
          }

          if (isMatch
            && (DestinationLabel == null || destinationNode.Type == DestinationLabel)
            && (DestinationPropertyFilter == null || DestinationPropertyFilter.IsMatch(destinationNode)))
          {
            yield return row;
          }
        }
      }
    }
  }
}
