using Cyphoid.Core.Execution;

namespace Cyphoid.Tests.TestBackend
{
  internal class NodeScanOperator : OperatorBase, IOperator
  {
    public NodeScanOperator(InMemoryGraph graph)
      : base(graph)
    {
    }


    async IAsyncEnumerable<Row> IOperator.ExecuteAsync(QueryContext context)
    {
      foreach (var node in Graph.Nodes)
      {
        yield return new Row(1);
      }
    }
  }
}
