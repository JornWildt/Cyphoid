using Cyphoid.Core.Execution;

namespace Cyphoid.Tests.TestBackend
{
  internal class TestOperatorFactory : IOperatorFactory
  {
    InMemoryGraph Graph;

    
    public TestOperatorFactory(InMemoryGraph graph)
    {
      Graph = graph;
    }

    
    IOperator IOperatorFactory.BuildProjection(IOperator input)
    {
      return new ProjectionOperator(Graph, input);
    }


    IOperator IOperatorFactory.BuildNodeScan()
    {
      return new NodeScanOperator(Graph);
    }
  }
}
