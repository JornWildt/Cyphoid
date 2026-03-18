using Cyphoid.Core;
using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Tests.TestBackend
{
  internal class NodeScanOperator : OperatorBase, IOperator
  {
    VariableDefinition Variable;
    string? Label;
    PropertyFilter? PropertyFilter;

    public NodeScanOperator(
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


    async IAsyncEnumerable<Row> IOperator.ExecuteAsync(QueryContext context)
    {
      foreach (var node in Graph.Nodes)
      {
        if ((Label == null || node.Value.Labels.Contains(Label)) && 
          (PropertyFilter == null || PropertyMatch(node.Value)))
        {
          var row = new Row(context.RowSize);
          // FIXME: Null values???
          var attributes = node.Value.Properties.ToDictionary(a => a.Key, a => a.Value!);
          row.Nodes[Variable.SlotIndex] = new GraphNode(attributes);

          yield return row;
        }
      }
    }

    
    private bool PropertyMatch(InMemoryGraph.Node node)
    {
      foreach (var p in PropertyFilter!.Conditions)
      {
        if (!node.Properties.TryGetValue(p.PropertyName, out var propertyValue))
          return false;

        // FIXME: Expr must be evaluated to constant value
        if (!p.Value.Equals(propertyValue))
          return false;
      }

      return true;
    }
  }
}
