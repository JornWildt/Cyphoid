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


    protected override IRow<string> NewRow(IRowColumn[] matchColumns)
    {
      return new Row<string>(matchColumns);
    }


    protected override IOperator<string> BuildNodeScan(
      VariableDefinition variable, 
      string? label, 
      PropertyFilter? propertyFilter,
      string? targetRelationshipLabel,
      IRowColumn[] matchColumns)
    {
      return new NodeScanTestOperator<string>(Graph, variable, label, propertyFilter, targetRelationshipLabel, matchColumns);
    }


    protected override IOperator<string> BuildExpandAll(
      IOperator<string> input,
      bool optional,
      VariableDefinition sourceVariable,
      ExpandDirectionType direction,
      string? relationLabel,
      VariableDefinition destinationVariable,
      string? destinationLabel,
      PropertyFilter? destinationPropertyFilter,
      IRowColumn[] matchColumns)
    {
      return new ExpandAllTestOperator<string>(
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
