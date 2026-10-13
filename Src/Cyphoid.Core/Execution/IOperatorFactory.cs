using Cyphoid.Core.Expressions;
using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Core.Execution
{
  public record PropertyFilter(IReadOnlyList<PropertyFilterCondition> Conditions)
  {
    public bool IsMatch(IGraphNode node)
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

  public enum ExpandDirectionType { Outgoing, Incoming, Unidirectional }

  public delegate MixedValue RowEvaluator(IRow row);


  public interface IOperatorFactory
  {
    IRow NewRow(IRowColumn[] matchColumns);

    IOperator BuildEmptyResult();

    IOperator BuildNodeScan(
      VariableDefinition variable,
      string? label,
      PropertyFilter? propertyFilter,
      string? targetRelationshipLabel,
      IRowColumn[] matchColumns);

    IOperator BuildExpandAll(
      IOperator input,
      bool optional,
      VariableDefinition sourceVariable,
      ExpandDirectionType direction,
      string? relationLabel,
      VariableDefinition destinationVariable,
      string? destinationLabel,
      PropertyFilter? destinationPropertyFilter,
      IRowColumn[] matchColumns);

    IOperator BuildExpandInto(
      IOperator input,
      bool optional,
      VariableDefinition? sourceVariable,
      ExpandDirectionType? direction,
      string? relationLabel,
      VariableDefinition destinationVariable,
      string? destinationLabel,
      PropertyFilter? destinationPropertyFilter);

    IOperator BuildCartesianProduct(
      IOperator operator1,
      IOperator operator2);

    IOperator BuildFilter(
      IOperator input,
      RowEvaluator evaluator);

    IOperator BuildLimit(
      IOperator input,
      int limit);

    IOperator BuildProjection(
      IOperator input,
      IReadOnlyList<ProjectionEvaluator> projections);

    IOperator BuildDistinct(
      IOperator input,
      List<GroupingEvaluator> groupings);

    IOperator BuildAggregationProjection(
      IOperator input,
      List<GroupingEvaluator> groupings,
      List<IAggregationEvaluator> aggregators,
      IRowColumn[] outputColumns);

    IOperator BuildOrderBy(
      IOperator input,
      IReadOnlyList<OrderByEvaluator> ordering);
  }
}
