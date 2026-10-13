using Cyphoid.Core;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;
using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Tests.TestBackend.Operators
{
  internal class NodeScanTestOperator : OperatorTestBase, IOperator
  {
    VariableDefinition Variable;
    string? Label;
    PropertyFilter? PropertyFilter;
    string? TargetRelationshipLabel;
    IRowColumn[] MatchColumns;

    public NodeScanTestOperator(
      InMemoryGraph graph,
      VariableDefinition variable,
      string? label,
      PropertyFilter? propertyFilter,
      string? targetRelationshipLabel,
      IRowColumn[] matchColumns)
      : base(graph)
    {
      Variable = variable;
      Label = label;
      PropertyFilter = propertyFilter;
      TargetRelationshipLabel = targetRelationshipLabel;
      MatchColumns = matchColumns;
    }


    async IAsyncEnumerable<IRow> IOperator.ExecuteAsync(IQueryContext context)
    {
      // Just to satisfy "await"
      await Task.Yield();

      foreach (var node in Graph.Nodes)
      {
        var hasExpectedRelationship = TargetRelationshipLabel != null
          ? node.Value.Outgoing.Any(o => o.Type == TargetRelationshipLabel)
          : true;

        if ((Label == null || node.Value.Labels.Contains(Label)) &&
          (PropertyFilter == null || PropertyMatch(PropertyFilter, node.Value)) &&
          hasExpectedRelationship)
        {
          IRow row = new Row(MatchColumns);
          var newNode = new GraphNode<string>(
            node.Value.Id,
            node.Value.Labels.First(),
            node.Value.Outgoing.Select(o => new GraphEdge<string>(o.Type, o.To.Id)).ToArray(),
            node.Value.Properties.ToDictionary(a => a.Key, a => a.Value));
          row.Values[Variable.SlotIndex] = MixedValue.GraphNode(newNode);

          yield return row;
        }
      }
    }
  }
}
