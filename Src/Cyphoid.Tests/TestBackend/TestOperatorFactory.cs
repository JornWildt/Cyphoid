using Cyphoid.Core;
using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Tests.TestBackend.Operators;

namespace Cyphoid.Tests.TestBackend
{
  internal class TestOperatorFactory : ReferenceOperatorFactory<string>
  {
    InMemoryGraph Graph;


    public TestOperatorFactory(InMemoryGraph graph)
    {
      Graph = graph;
    }


    protected override IOperator<string> BuildNodeScan(VariableDefinition variable, string? label, PropertyFilter? propertyFilter)
    {
      return new NodeScanTestOperator<string>(Graph, variable, label, propertyFilter);
    }


    protected override IOperator<string> BuildExpandAll(
      IOperator<string> input,
      VariableDefinition sourceVariable,
      ExpandDirectionType direction,
      string? relationLabel,
      VariableDefinition destinationVariable,
      string? destinationLabel,
      PropertyFilter? destinationPropertyFilter)
    {
      return new ExpandAllTestOperator<string>(
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
