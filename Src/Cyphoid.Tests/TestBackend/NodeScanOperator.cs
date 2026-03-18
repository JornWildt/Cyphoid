using Cyphoid.Core;
using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Tests.TestBackend
{
  internal class NodeScanOperator : OperatorBase, IOperator
  {
    VariableDefinition Variable;
    string? Label;
    PropertyMapNode? PropertyMap;

    public NodeScanOperator(
      InMemoryGraph graph,
      VariableDefinition variable,
      string? label,
      PropertyMapNode? propertyMap)
      : base(graph)
    {
      Variable = variable;
      Label = label;
      PropertyMap = propertyMap;
    }


    async IAsyncEnumerable<Row> IOperator.ExecuteAsync(QueryContext context)
    {
      foreach (var node in Graph.Nodes)
      {
        if (Label == null || node.Value.Labels.Contains(Label))
        {
          var row = new Row(context.RowSize);
          // FIXME: Null values???
          var attributes = node.Value.Properties.ToDictionary(a => a.Key, a => a.Value!);
          row.Nodes[Variable.SlotIndex] = new GraphNode(attributes);

          yield return row;
        }
      }
    }
  }
}
