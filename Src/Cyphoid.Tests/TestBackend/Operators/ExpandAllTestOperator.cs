using Cyphoid.Core;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;
using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Tests.TestBackend.Operators
{
  internal class ExpandAllTestOperator<TId> : OperatorTestBase, IOperator<string>
  {
    IOperator<string> Input;
    bool Optional;
    VariableDefinition SourceVariable;
    ExpandDirectionType Direction;
    string? RelationLabel;
    VariableDefinition DestinationVariable;
    string? DestinationLabel;
    PropertyFilter? DestinationPropertyFilter;
    IRowColumn[] MatchColumns;


    public ExpandAllTestOperator(
      InMemoryGraph graph,
      IOperator<string> input,
      bool optional,
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
      Optional = optional;
      SourceVariable = sourceVariable;
      Direction = direction;
      RelationLabel = relationLabel;
      DestinationVariable = destinationVariable;
      DestinationLabel = destinationLabel;
      DestinationPropertyFilter = destinationPropertyFilter;
      MatchColumns = matchColumns;
    }


    IAsyncEnumerable<IRow<string>> IOperator<string>.ExecuteAsync(IQueryContext context)
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


    async IAsyncEnumerable<IRow<string>> OutgoingAsync(IQueryContext context)
    {
      await foreach (var row in Input.ExecuteAsync(context))
      {
        bool hasMatch = false;

        var sourceVariable = row.Values[SourceVariable.SlotIndex];
        if (sourceVariable != null)
        {
          var sourceNode = sourceVariable.Value.AsGraphNode<string>();
          var matchingEdges = sourceNode.Edges
            .Where(e => RelationLabel == null || e.Type == RelationLabel);

          var targetIds = matchingEdges
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
                var newRow = row.Clone(MatchColumns);
                var newNode = new GraphNode<string>(
                  targetId,
                  targetNode.Labels.First(),
                  targetNode.Outgoing.Select(o => new GraphEdge<string>(o.Type, o.To.Id)).ToArray(),
                  targetNode.Properties);
                newRow.Values[DestinationVariable.SlotIndex] = MixedValue.GraphNode(newNode);

                hasMatch = true;
                yield return newRow;
              }
            }
          }
        }

        if (!hasMatch && Optional)
        {
          var newRow = row.Clone(MatchColumns);
          yield return newRow;
        }
      }
    }


    async IAsyncEnumerable<IRow<string>> IncomingAsync(IQueryContext context)
    {
      await foreach (var row in Input.ExecuteAsync(context))
      {
        // Note that "incoming" goes to the source in the relationship
        // FIXME: maybe call them left/right instead?

        var sourceVariable = row.Values[SourceVariable.SlotIndex];

        if (sourceVariable != null)
        {
          var sourceNode = sourceVariable.Value.AsGraphNode<string>();
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
                var newRow = row.Clone(row.Columns);
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
