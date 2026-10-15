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
    string? RelationLabel;
    VariableDefinition DestinationVariable;
    string? DestinationLabel;
    PropertyFilter? DestinationPropertyFilter;
    IRowColumn[] MatchColumns;


    public ExpandAllTestOperator(
      InMemoryGraph graph,
      IOperator input,
      VariableDefinition sourceVariable,
      ExpandDirectionType direction,
      string? relationLabel,
      VariableDefinition destinationVariable,
      string? destinationLabel,
      PropertyFilter? destinationPropertyFilter,
      IRowColumn[] matchColumns)
      : base(graph)
    {
      Input = input;
      SourceVariable = sourceVariable;
      Direction = direction;
      RelationLabel = relationLabel;
      DestinationVariable = destinationVariable;
      DestinationLabel = destinationLabel;
      DestinationPropertyFilter = destinationPropertyFilter;
      MatchColumns = matchColumns;
    }


    IAsyncEnumerable<IRow> IOperator.ExecuteAsync(IQueryContext context)
    {
      if (Direction == ExpandDirectionType.Outgoing)
        return OutgoingAsync(context);
      else if (Direction == ExpandDirectionType.Incoming)
        return IncomingAsync(context);
      else if (Direction == ExpandDirectionType.Unidirectional)
        return OutgoingAsync(context).Concat(IncomingAsync(context));
      else
        throw new NotImplementedException();
    }


    async IAsyncEnumerable<IRow> OutgoingAsync(IQueryContext context)
    {
      await foreach (var row in Input.ExecuteAsync(context))
      {
        var sourceVariable = row.Values[SourceVariable.SlotIndex];
        if (sourceVariable != null)
        {
          var sourceNode = sourceVariable.Value.AsGraphNode();
          var matchingEdges = sourceNode.Edges
            .Where(e => RelationLabel == null || e.Type == RelationLabel);

          var targetIds = matchingEdges
            .Cast<GraphEdge<string>>()
            .Select(e => e.Target)
            .Distinct()
            .ToList();

          foreach (var targetId in targetIds)
          {
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


    async IAsyncEnumerable<IRow> IncomingAsync(IQueryContext context)
    {
      await foreach (var row in Input.ExecuteAsync(context))
      {
        // Note that "incoming" goes to the source in the relationship
        // FIXME: maybe call them left/right instead?

        var sourceVariable = row.Values[SourceVariable.SlotIndex];

        if (sourceVariable != null)
        {
          var sourceNode = (GraphNode<string>)sourceVariable.Value.AsGraphNode();
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
