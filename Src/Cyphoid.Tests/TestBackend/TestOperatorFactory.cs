using Cyphoid.Core;
using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;

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


    IOperator IOperatorFactory.BuildNodeScan(
      VariableDefinition variable,
      string? label,
      PropertyMapNode? propertyMap)
    {
      return new NodeScanOperator(Graph, variable, label, propertyMap);
    }
  }
}
