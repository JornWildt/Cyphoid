using System.Security.Cryptography;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.Execution
{
  public record PropertyFilter(IReadOnlyList<PropertyFilterCondition> Conditions)
  {
    public bool IsMatch<TId>(IGraphNode<TId> node) where TId : IEquatable<TId>
    {
      foreach (var p in Conditions)
      {
        if (!node.Attributes.TryGetValue(p.PropertyName, out var propertyValue))
          return false;

        if (!p.Value.Equals(propertyValue))
          return false;
      }

      return true;
    }

  }

  public record PropertyFilterCondition(string PropertyName, MixedValue Value);

  public enum ExpandDirectionType { Outgoing, Incoming }

  public delegate MixedValue RowEvaluator<TId>(IRow<TId> row) where TId : IEquatable<TId>;

  public interface IOperatorFactory<TId> where TId : IEquatable<TId>
  {
    IOperator<TId> BuildEmptyResult();

    IOperator<TId> BuildNodeScan(
      VariableDefinition variable,
      string? label,
      PropertyFilter? propertyFilter,
      IRowColumn[] matchColumns);

    IOperator<TId> BuildExpandAll(
      IOperator<TId> input,
      VariableDefinition sourceVariable,
      ExpandDirectionType direction,
      string? relationLabel,
      VariableDefinition destinationVariable,
      string? destinationLabel,
      PropertyFilter? destinationPropertyFilter);

    IOperator<TId> BuildExpandInto(
      IOperator<TId> input,
      VariableDefinition? sourceVariable,
      ExpandDirectionType? direction,
      string? relationLabel,
      VariableDefinition destinationVariable,
      string? destinationLabel,
      PropertyFilter? destinationPropertyFilter);

    IOperator<TId> BuildCartesianProduct(
      IOperator<TId> operator1, 
      IOperator<TId> operator2);

    IOperator<TId> BuildFilter(
      IOperator<TId> input,
      RowEvaluator<TId> evaluator);

    IOperator<TId> BuildLimit(
      IOperator<TId> input,
      int limit);

    IOperator<TId> BuildProjection(
      IOperator<TId> input,
      IReadOnlyList<ProjectionEvaluator<TId>> projections);
  }
}
