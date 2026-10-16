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


    protected override IOperator BuildNodeScan(
      VariableDefinition variable, 
      string? label, 
      PropertyFilter? propertyFilter,
      string? targetRelationshipLabel,
      RowSchema matchColumns)
    {
      return new NodeScanTestOperator(Graph, variable, label, propertyFilter, targetRelationshipLabel, matchColumns);
    }


    protected override IOperator BuildExpandAll(
      IOperator input,
      VariableDefinition sourceVariable,
      ExpandDirectionType direction,
      string? relationLabel,
      VariableDefinition destinationVariable,
      string? destinationLabel,
      PropertyFilter? destinationPropertyFilter,
      RowSchema matchColumns)
    {
      return new ExpandAllTestOperator(
        Graph,
        input,
        sourceVariable,
        direction,
        relationLabel,
        destinationVariable,
        destinationLabel,
        destinationPropertyFilter,
        matchColumns);
    }
  }
}
