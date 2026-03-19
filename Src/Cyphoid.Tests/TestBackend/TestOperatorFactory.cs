using Cyphoid.Core;
using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Tests.TestBackend.Operators;

namespace Cyphoid.Tests.TestBackend
{
  internal class TestOperatorFactory : ReferenceOperatorFactory
  {
    InMemoryGraph Graph;


    public TestOperatorFactory(InMemoryGraph graph)
    {
      Graph = graph;
    }


    protected override IOperator BuildNodeScan(VariableDefinition variable, string? label, PropertyFilter? propertyFilter)
    {
      return new NodeScanTestOperator(Graph, variable, label, propertyFilter);
    }


    protected override IOperator BuildExpand(
      IOperator input,
      VariableDefinition sourceVariable,
      ExpandDirectionType direction,
      string? relationLabel,
      VariableDefinition destinationVariable,
      string? destinationLabel,
      PropertyFilter? destinationPropertyFilter)
    {
      return new ExpandTestOperator(
        Graph,
        input,
        sourceVariable,
        direction,
        relationLabel,
        destinationVariable,
        destinationLabel,
        destinationPropertyFilter);
    }
  }
}
