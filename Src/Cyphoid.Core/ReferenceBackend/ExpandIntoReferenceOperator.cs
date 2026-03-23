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
        var destinationVariable = row.Variables[DestinationVariable.SlotIndex];
        if (destinationVariable != null)
        {
          var destinationNode = destinationVariable.Value.AsGraphNode<TId>();

          bool isMatch = false;

          if (SourceVariable != null)
          {
            var sourceVariable = row.Variables[SourceVariable.SlotIndex];
            if (sourceVariable != null)
            {
              var sourceNode = sourceVariable.Value.AsGraphNode<TId>();
              if (Direction == ExpandDirectionType.Outgoing)
              {
                var matchingEdges = sourceNode.Edges
                  .Where(e => RelationLabel == null || e.Key == RelationLabel);

                // FIXME: Edge destination must be TId (not string)
                if (matchingEdges.Any(e => EqualityComparer<TId>.Default.Equals(e.Value, destinationNode.Id)))
                  isMatch = true;
              }
              else
              {
                var matchingEdges = destinationNode.Edges
                  .Where(e => RelationLabel == null || e.Key == RelationLabel);

                // FIXME: Edge destination must be TId (not string)
                if (matchingEdges.Any(e => EqualityComparer<TId>.Default.Equals(e.Value, sourceNode.Id)))
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
