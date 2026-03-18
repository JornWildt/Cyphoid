using Cyphoid.Core.Expressions;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Execution
{

  public record PropertyFilter(IReadOnlyList<PropertyFilterCondition> Conditions);

  public record PropertyFilterCondition(string PropertyName, MixedValue Value);

  public interface IOperatorFactory
  {
    IOperator BuildNodeScan(
      VariableDefinition variable,
      string? label,
      PropertyFilter? propertyFilter);

    IOperator BuildExpand(
      IOperator source,
      VariableDefinition sourceVariable,
      VariableDefinition destinationVariable,
      string? destinationLabel,
      PropertyFilter? destinationPropertyFilter);

    IOperator BuildProjection(IOperator input);
  }
}
