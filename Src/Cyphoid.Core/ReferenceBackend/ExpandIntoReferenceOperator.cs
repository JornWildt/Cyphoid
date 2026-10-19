using System.Runtime.CompilerServices;
using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  // FIXME: Source not nullable (that would be cartesian product????)


  public record ExpandIntoReferenceOperator(
    IOperator Input,
    VariableDefinition? SourceVariable,
    ExpandDirectionType? Direction,
    string? RelationshipType,
    VariableDefinition DestinationVariable,
    string? DestinationLabel,
    PropertyFilter? DestinationPropertyFilter) : IOperator
  {
    async IAsyncEnumerable<IRow> IOperator.ExecuteAsync(IQueryContext context, [EnumeratorCancellation] CancellationToken ct)
    {
      ct.ThrowIfCancellationRequested();
      await foreach (var row in Input.ExecuteAsync(context, ct).WithCancellation(ct))
      {
        ct.ThrowIfCancellationRequested();
        var destinationVariable = row.Values[DestinationVariable.SlotIndex];
        if (destinationVariable != null && !destinationVariable.Value.IsNull())
        {
          var destinationNode = destinationVariable.Value.AsGraphNode();

          bool isMatch = false;

          if (SourceVariable != null)
          {
            var sourceVariable = row.Values[SourceVariable.SlotIndex];
            if (sourceVariable != null && !sourceVariable.Value.IsNull())
            {
              var sourceNode = sourceVariable.Value.AsGraphNode();
              if (Direction == ExpandDirectionType.Outgoing)
              {
                var matchingEdges = sourceNode.Edges
                  .Where(e => RelationshipType == null || e.Type == RelationshipType);

                if (matchingEdges.Any(e => e.RefersTo(destinationNode)))
                  isMatch = true;
              }
              else
              {
                var matchingEdges = destinationNode.Edges
                  .Where(e => RelationshipType == null || e.Type == RelationshipType);

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
