using Cyphoid.Core;
using Cyphoid.Core.Execution;
using System.Reflection.Emit;
using static Cyphoid.Tests.TestBackend.InMemoryGraph;

namespace Cyphoid.Tests.TestBackend.Operators
{
  internal class ExpandOperator : OperatorBase, IOperator
  {
    IOperator Input;
    VariableDefinition SourceVariable;
    VariableDefinition DestinationVariable;
    string? DestinationLabel;
    PropertyFilter? DestinationPropertyFilter;


    public ExpandOperator(
      InMemoryGraph graph, 
      IOperator input,
      VariableDefinition sourceVariable,
      VariableDefinition destinationVariable,
      string? destinationLabel,
      PropertyFilter? destinationPropertyFilter)
      : base(graph)
    {
      Input = input;
      SourceVariable = sourceVariable;
      DestinationVariable = destinationVariable;
      DestinationLabel = destinationLabel;
      DestinationPropertyFilter = destinationPropertyFilter;
    }

    
    async IAsyncEnumerable<Row> IOperator.ExecuteAsync(QueryContext context)
    {
      await foreach (var row in Input.ExecuteAsync(context))
      {
        var sourceNode = row.Nodes[SourceVariable.SlotIndex];

        var matchingEdges = sourceNode.Edges
          .Where(e => true);

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
              newRow.Nodes[DestinationVariable.SlotIndex] = new GraphNode(
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
