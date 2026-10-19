using System.Runtime.CompilerServices;
using Cyphoid.Core;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;
using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Tests.TestBackend.Operators
{
  internal class ExpandAllTestOperator : OperatorTestBase, IOperator
  {
    IOperator Input;
    VariableDefinition SourceVariable;
    ExpandDirectionType Direction;
    string? RelationshipType;
    VariableDefinition DestinationVariable;
    string? DestinationLabel;
    PropertyFilter? DestinationPropertyFilter;
    RowSchema MatchColumns;


    public ExpandAllTestOperator(
      InMemoryGraph graph,
      IOperator input,
      VariableDefinition sourceVariable,
      ExpandDirectionType direction,
      string? relationshipType,
      VariableDefinition destinationVariable,
      string? destinationLabel,
      PropertyFilter? destinationPropertyFilter,
      RowSchema matchColumns)
      : base(graph)
    {
      Input = input;
      SourceVariable = sourceVariable;
      Direction = direction;
      RelationshipType = relationshipType;
      DestinationVariable = destinationVariable;
      DestinationLabel = destinationLabel;
      DestinationPropertyFilter = destinationPropertyFilter;
      MatchColumns = matchColumns;
    }


    IAsyncEnumerable<IRow> IOperator.ExecuteAsync(IQueryContext context, CancellationToken ct)
    {
      ct.ThrowIfCancellationRequested();
      if (Direction == ExpandDirectionType.Outgoing)
        return OutgoingAsync(context, ct);
      else if (Direction == ExpandDirectionType.Incoming)
        return IncomingAsync(context, ct);
      else if (Direction == ExpandDirectionType.Unidirectional)
        return OutgoingAsync(context, ct).Concat(IncomingAsync(context, ct));
      else
        throw new NotImplementedException();
    }


    async IAsyncEnumerable<IRow> OutgoingAsync(IQueryContext context, [EnumeratorCancellation] CancellationToken ct)
    {
      ct.ThrowIfCancellationRequested();
      await foreach (var row in Input.ExecuteAsync(context, ct).WithCancellation(ct))
      {
        ct.ThrowIfCancellationRequested();
        var sourceVariable = row.Values[SourceVariable.SlotIndex];
        if (sourceVariable != null && !sourceVariable.Value.IsNull())
        {
          var sourceNode = sourceVariable.Value.AsGraphNode();
          var matchingEdges = sourceNode.Edges
            .Where(e => RelationshipType == null || e.Type == RelationshipType);

          var targetIds = matchingEdges
            .Cast<GraphEdge<string>>()
            .Select(e => e.Target)
            .Distinct()
            .ToList();

          foreach (var targetId in targetIds)
          {
            ct.ThrowIfCancellationRequested();
            if (Graph.TryGetNode(targetId, out var targetNode))
            {
              if ((DestinationLabel == null || targetNode.Labels.Contains(DestinationLabel)) &&
                (DestinationPropertyFilter == null || PropertyMatch(DestinationPropertyFilter, targetNode)))
              {
                var newRow = row.CopyAndExpand(MatchColumns);
                var newNode = new GraphNode<string>(
                  targetId,
                  targetNode.Labels.First(),
                  targetNode.Outgoing.Select(o => new GraphEdge<string>(o.Type, o.To.Id)).ToArray(),
                  targetNode.Properties);
                newRow.Values[DestinationVariable.SlotIndex] = MixedValue.GraphNode(newNode);
                yield return newRow;
              }
            }
          }
        }
      }
    }


    async IAsyncEnumerable<IRow> IncomingAsync(IQueryContext context, [EnumeratorCancellation] CancellationToken ct)
    {
      ct.ThrowIfCancellationRequested();
      await foreach (var row in Input.ExecuteAsync(context, ct).WithCancellation(ct))
      {
        ct.ThrowIfCancellationRequested();
        // Note that "incoming" goes to the source in the relationship
        // FIXME: maybe call them left/right instead?

        var sourceVariable = row.Values[SourceVariable.SlotIndex];

        if (sourceVariable != null && !sourceVariable.Value.IsNull())
        {
          var sourceNode = (GraphNode<string>)sourceVariable.Value.AsGraphNode();
          var sourceId = sourceNode.Id;

          var matchingEdges = Graph.Incoming(sourceId)
            .Where(e => RelationshipType == null || e.Type == RelationshipType);

          // The IDs of the nodes that target the source
          var targetIds = matchingEdges
            .Select(e => e.From.Id)
            .Distinct()
            .ToList();

          foreach (var targetId in targetIds)
          {
            ct.ThrowIfCancellationRequested();
            if (Graph.TryGetNode(targetId, out var targetNode))
            {
              if ((DestinationLabel == null || targetNode.Labels.Contains(DestinationLabel)) &&
                (DestinationPropertyFilter == null || PropertyMatch(DestinationPropertyFilter, targetNode)))
              {
                var newRow = row.CopyAndExpand(MatchColumns);
                var newNode = new GraphNode<string>(
                  targetId,
                  targetNode.Labels.First(),
                  targetNode.Outgoing.Select(o => new GraphEdge<string>(o.Type, o.To.Id)).ToArray(),
                  targetNode.Properties);
                newRow.Values[DestinationVariable.SlotIndex] = MixedValue.GraphNode(newNode);
                yield return newRow;
              }
            }
          }
        }
      }
    }
  }
}
