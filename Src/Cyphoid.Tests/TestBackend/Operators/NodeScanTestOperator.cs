using Cyphoid.Core;
using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Tests.TestBackend.Operators
{
  internal class NodeScanTestOperator : OperatorTestBase, IOperator
  {
    VariableDefinition Variable;
    string? Label;
    PropertyFilter? PropertyFilter;

    public NodeScanTestOperator(
      InMemoryGraph graph,
      VariableDefinition variable,
      string? label,
      PropertyFilter? propertyFilter)
      : base(graph)
    {
      Variable = variable;
      Label = label;
      PropertyFilter = propertyFilter;
    }


    async IAsyncEnumerable<IRow> IOperator.ExecuteAsync(IQueryContext context)
    {
      // Just to satisfy "await"
      await Task.Yield();

      foreach (var node in Graph.Nodes)
      {
        if ((Label == null || node.Value.Labels.Contains(Label)) &&
          (PropertyFilter == null || PropertyMatch(PropertyFilter, node.Value)))
        {
          IRow row = new Row(context.RowSize);
          row.Nodes[Variable.SlotIndex] = new GraphNode(
            node.Value.Id,
            node.Value.Outgoing.ToDictionary(e => e.Type, e => e.To.Id),
            node.Value.Properties.ToDictionary(a => a.Key, a => a.Value));

          yield return row;
        }
      }
    }
  }
}
