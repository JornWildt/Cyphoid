using Cyphoid.Core;
using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Tests.TestBackend.Operators
{
  internal class ExpandAllTestOperator<TId> : OperatorTestBase, IOperator<string>
  {
    IOperator<string> Input;
    VariableDefinition SourceVariable;
    ExpandDirectionType Direction;
    string? RelationLabel;
    VariableDefinition DestinationVariable;
    string? DestinationLabel;
    PropertyFilter? DestinationPropertyFilter;


    public ExpandAllTestOperator(
      InMemoryGraph graph,
      IOperator<string> input,
      VariableDefinition sourceVariable,
      ExpandDirectionType direction,
      string? relationLabel,
      VariableDefinition destinationVariable,
      string? destinationLabel,
      PropertyFilter? destinationPropertyFilter)
      : base(graph)
    {
      Input = input;
      SourceVariable = sourceVariable;
      Direction = direction;
      RelationLabel = relationLabel;
      DestinationVariable = destinationVariable;
      DestinationLabel = destinationLabel;
      DestinationPropertyFilter = destinationPropertyFilter;
    }


    IAsyncEnumerable<IRow<string>> IOperator<string>.ExecuteAsync(IQueryContext context)
    {
      if (Direction == ExpandDirectionType.Outgoing)
        return OutgoingAsync(context);
      else if (Direction == ExpandDirectionType.Incoming)
        return IncomingAsync(context);
      else
        throw new NotImplementedException();
    }


    async IAsyncEnumerable<IRow<string>> OutgoingAsync(IQueryContext context)
    {
      await foreach (var row in Input.ExecuteAsync(context))
      {
        var sourceNode = row.Nodes[SourceVariable.SlotIndex];
        if (sourceNode != null)
        {
          var matchingEdges = sourceNode.Edges
            .Where(e => RelationLabel == null || e.Key == RelationLabel);

          var targetIds = matchingEdges
            .Select(e => e.Value)
            .Distinct()
            .ToList();

          foreach (var targetId in targetIds)
          {
            if (Graph.TryGetNode(targetId, out var targetNode))
            {
              if ((DestinationLabel == null || targetNode.Labels.Contains(DestinationLabel)) &&
                (DestinationPropertyFilter == null || PropertyMatch(DestinationPropertyFilter, targetNode)))
              {
                var newRow = row.Clone();
                newRow.Nodes[DestinationVariable.SlotIndex] = new GraphNode<string>(
                  targetId,
                  targetNode.Labels.First(),
                  targetNode.Outgoing.ToDictionary(e => e.Type, e => e.To.Id),
                  targetNode.Properties);
                yield return newRow;
              }
            }
          }
        }
      }
    }


    async IAsyncEnumerable<IRow<string>> IncomingAsync(IQueryContext context)
    {
      await foreach (var row in Input.ExecuteAsync(context))
      {
        // Note that "incoming" goes to the source in the relationship
        // FIXME: maybe call them left/right instead?

        var sourceNode = row.Nodes[SourceVariable.SlotIndex];

        if (sourceNode != null)
        {
          var sourceId = sourceNode.Id;

          var matchingEdges = Graph.Incoming(sourceId)
            .Where(e => RelationLabel == null || e.Type == RelationLabel);

          // The IDs of the nodes that target the source
          var targetIds = matchingEdges
            .Select(e => e.From.Id)
            .Distinct()
            .ToList();

          foreach (var targetId in targetIds)
          {
            if (Graph.TryGetNode(targetId, out var targetNode))
            {
              if ((DestinationLabel == null || targetNode.Labels.Contains(DestinationLabel)) &&
                (DestinationPropertyFilter == null || PropertyMatch(DestinationPropertyFilter, targetNode)))
              {
                var newRow = row.Clone();
                newRow.Nodes[DestinationVariable.SlotIndex] = new GraphNode<string>(
                  targetId,
                  targetNode.Labels.First(),
                  targetNode.Outgoing.ToDictionary(e => e.Type, e => e.To.Id),
                  targetNode.Properties);
                yield return newRow;
              }
            }
          }
        }
      }
    }
  }
}
