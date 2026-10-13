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


    protected override IRow NewRow(IRowColumn[] matchColumns)
    {
      return new Row(matchColumns);
    }


    protected override IOperator BuildNodeScan(
      VariableDefinition variable, 
      string? label, 
      PropertyFilter? propertyFilter,
      string? targetRelationshipLabel,
      IRowColumn[] matchColumns)
    {
      return new NodeScanTestOperator(Graph, variable, label, propertyFilter, targetRelationshipLabel, matchColumns);
    }


    protected override IOperator BuildExpandAll(
      IOperator input,
      bool optional,
      VariableDefinition sourceVariable,
      ExpandDirectionType direction,
      string? relationLabel,
      VariableDefinition destinationVariable,
      string? destinationLabel,
      PropertyFilter? destinationPropertyFilter,
      IRowColumn[] matchColumns)
    {
      return new ExpandAllTestOperator(
        Graph,
        input,
        optional,
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
