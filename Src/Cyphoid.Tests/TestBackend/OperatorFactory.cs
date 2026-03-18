using Cyphoid.Core;
using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Tests.TestBackend
{
  internal class OperatorFactory : IOperatorFactory
  {
    InMemoryGraph Graph;

    
    public OperatorFactory(InMemoryGraph graph)
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
      PropertyFilter? propertyFilter)
    {
      return new NodeScanOperator(Graph, variable, label, propertyFilter);
    }
  }
}
