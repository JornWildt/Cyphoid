using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.Execution
{
  public record PropertyFilter(IReadOnlyList<PropertyFilterCondition> Conditions);

  public record PropertyFilterCondition(string PropertyName, MixedValue Value);

  public enum ExpandDirectionType { Outgoing, Incoming }

  public delegate MixedValue RowEvaluator<TId>(IRow<TId> row) where TId : IEquatable<TId>;

  public interface IOperatorFactory<TId> where TId : IEquatable<TId>
  {
    IOperator<TId> BuildEmptyResult();

    IOperator<TId> BuildNodeScan(
      VariableDefinition variable,
      string? label,
      PropertyFilter? propertyFilter);

    IOperator<TId> BuildExpand(
      IOperator<TId> input,
      VariableDefinition sourceVariable,
      ExpandDirectionType direction,
      string? relationLabel,
      VariableDefinition destinationVariable,
      string? destinationLabel,
      PropertyFilter? destinationPropertyFilter);

    IOperator<TId> BuildFilter(
      IOperator<TId> input,
      RowEvaluator<TId> evaluator);

    IOperator<TId> BuildLimit(
      IOperator<TId> input,
      int limit);

    IProjectionOperator BuildProjection(
      IOperator<TId> input,
      IReadOnlyList<ProjectionEvaluator<TId>> projections);
  }
}
