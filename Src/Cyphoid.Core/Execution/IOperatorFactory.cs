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
        var exists = node.Attributes.TryGetValue(p.PropertyName, out var propertyValue);
        if (!p.IsMatch(exists, MixedValue.FromObject(propertyValue)))
          return false;
      }

      return true;
    }

  }

  /// <summary>Identifies the semantics a backend must use for a property equality.</summary>
  public enum PropertyEquality { Pattern, Query }

  /// <summary>
  /// A bound, row-independent property equality. Query constraints come from WHERE;
  /// use IsMatch rather than object.Equals so missing properties and maps retain
  /// the engine's query semantics. Backends may index these constraints only when
  /// their index implements the same semantics (including numeric and null values).
  /// </summary>
  public record PropertyFilterCondition(string PropertyName, MixedValue Value,
    PropertyEquality Equality = PropertyEquality.Pattern)
  {
    public bool IsMatch(bool exists, MixedValue propertyValue)
    {
      if (Equality == PropertyEquality.Pattern)
        return exists && !Value.IsNull() && !propertyValue.IsNull() && Value.Equals(propertyValue);
      var equal = MixedValue.QueryEquals(exists ? propertyValue : MixedValue.Null(), Value);
      // Unknown must reach the residual evaluator. In particular, do not hide
      // its diagnostics when this equality is an operand of a boolean expression.
      return equal.IsNull() || equal.AsBool();
    }
  }

  public enum ExpandDirectionType { Outgoing, Incoming, Unidirectional }

  public delegate MixedValue RowEvaluator(IRow row);


  public interface IOperatorFactory
  {
    IOperator BuildEmptyResult();

    IOperator BuildNodeScan(
      VariableDefinition variable,
      string? label,
      PropertyFilter? propertyFilter,
      string? targetRelationshipLabel,
      RowSchema matchColumns);

    IOperator BuildExpandAll(
      IOperator input,
      VariableDefinition sourceVariable,
      ExpandDirectionType direction,
      string? relationLabel,
      VariableDefinition destinationVariable,
      string? destinationLabel,
      PropertyFilter? destinationPropertyFilter,
      RowSchema matchColumns);

    IOperator BuildExpandInto(
      IOperator input,
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
      RowSchema outputColumns);

    IOperator BuildOrderBy(
      IOperator input,
      IReadOnlyList<OrderByEvaluator> ordering);
  }
}
