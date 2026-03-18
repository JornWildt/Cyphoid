using Cyphoid.Core;
using Cyphoid.Core.Execution;
using Cyphoid.Tests.TestBackend.Operators;

namespace Cyphoid.Tests.TestBackend
{
  internal class OperatorFactory : IOperatorFactory
  {
    InMemoryGraph Graph;


    public OperatorFactory(InMemoryGraph graph)
    {
      Graph = graph;
    }

    
    IOperator IOperatorFactory.BuildEmptyResult()
    {
      return new EmptyResultOperator();
    }


    IOperator IOperatorFactory.BuildNodeScan(
      VariableDefinition variable,
      string? label,
      PropertyFilter? propertyFilter)
    {
      return new NodeScanOperator(Graph, variable, label, propertyFilter);
    }


    IOperator IOperatorFactory.BuildExpand(
      IOperator input,
      VariableDefinition sourceVariable,
      VariableDefinition destinationVariable,
      string? destinationLabel,
      PropertyFilter? destinationPropertyFilter)
    {
      return new ExpandOperator(
        Graph,
        input,
        sourceVariable,
        destinationVariable,
        destinationLabel,
        destinationPropertyFilter);
    }


    IOperator IOperatorFactory.BuildLimit(IOperator input, int limit)
    {
      return new LimitOperator(Graph, input, limit);
    }


    IProjectionOperator IOperatorFactory.BuildProjection(
      IOperator input,
      IReadOnlyList<ProjectionEvaluator> projections)
    {
      return new ProjectionOperator(Graph, input, projections);
    }
  }
}
