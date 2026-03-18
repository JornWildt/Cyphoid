using Cyphoid.Core;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;
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
      ExpandDirectionType direction,
      string? relationLabel,
      VariableDefinition destinationVariable,
      string? destinationLabel,
      PropertyFilter? destinationPropertyFilter)
    {
      return new ExpandOperator(
        Graph,
        input,
        sourceVariable,
        direction,
        relationLabel,
        destinationVariable,
        destinationLabel,
        destinationPropertyFilter);
    }


    IOperator IOperatorFactory.BuildFilter(IOperator input, RowEvaluator evaluator)
    {
      return new FilterOperator(Graph, input, evaluator);
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
