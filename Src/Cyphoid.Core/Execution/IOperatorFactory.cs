using Cyphoid.Core.Expressions;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Execution
{
  public record PropertyFilter(IReadOnlyList<PropertyFilterCondition> Conditions);

  public record PropertyFilterCondition(string PropertyName, MixedValue Value);

  public enum ExpandDirectionType { Outgoing, Incoming }

  public delegate MixedValue RowEvaluator(Row row);

  public interface IOperatorFactory
  {
    IOperator BuildEmptyResult();

    IOperator BuildNodeScan(
      VariableDefinition variable,
      string? label,
      PropertyFilter? propertyFilter);

    IOperator BuildExpand(
      IOperator input,
      VariableDefinition sourceVariable,
      ExpandDirectionType direction,
      string? relationLabel,
      VariableDefinition destinationVariable,
      string? destinationLabel,
      PropertyFilter? destinationPropertyFilter);

    IOperator BuildFilter(
      IOperator input,
      RowEvaluator evaluator);

    IOperator BuildLimit(
      IOperator input,
      int limit);

    IProjectionOperator BuildProjection(
      IOperator input,
      IReadOnlyList<ProjectionEvaluator> projections);
  }
}
