using System.Data;
using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend.Aggregation;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.ReferenceBackend
{
  public abstract class ReferenceOperatorFactory<TId> : IOperatorFactory<TId> where TId : IEquatable<TId>
  {
    #region IOperatorFactory

    IRow<TId> IOperatorFactory<TId>.NewRow(IRowColumn[] matchColumns)
      => NewRow(matchColumns);

    IOperator<TId> IOperatorFactory<TId>.BuildEmptyResult()
      => BuildEmptyResult();

    IOperator<TId> IOperatorFactory<TId>.BuildNodeScan(VariableDefinition variable, string? label, PropertyFilter? propertyFilter, IRowColumn[] matchColumns)
      => BuildNodeScan(variable, label, propertyFilter, matchColumns);

    IOperator<TId> IOperatorFactory<TId>.BuildExpandAll(IOperator<TId> input, VariableDefinition sourceVariable, ExpandDirectionType direction, string? relationLabel, VariableDefinition destinationVariable, string? destinationLabel, PropertyFilter? destinationPropertyFilter)
      => BuildExpandAll(input, sourceVariable, direction, relationLabel, destinationVariable, destinationLabel, destinationPropertyFilter);

    IOperator<TId> IOperatorFactory<TId>.BuildExpandInto(IOperator<TId> input, VariableDefinition? sourceVariable, ExpandDirectionType? direction, string? relationLabel, VariableDefinition destinationVariable, string? destinationLabel, PropertyFilter? destinationPropertyFilter)
      => BuildExpandInto(input, sourceVariable, direction, relationLabel, destinationVariable, destinationLabel, destinationPropertyFilter);

    IOperator<TId> IOperatorFactory<TId>.BuildCartesianProduct(IOperator<TId> left, IOperator<TId> right)
      => BuildCartesianProduct(left, right);

    IOperator<TId> IOperatorFactory<TId>.BuildFilter(IOperator<TId> input, RowEvaluator<TId> evaluator)
      => BuildFilter(input, evaluator);

    IOperator<TId> IOperatorFactory<TId>.BuildLimit(IOperator<TId> input, int limit)
      => BuildLimit(input, limit);

    IOperator<TId> IOperatorFactory<TId>.BuildProjection(IOperator<TId> input, IReadOnlyList<ProjectionEvaluator<TId>> projections)
      => BuildProjection(input, projections);

    IOperator<TId> IOperatorFactory<TId>.BuildAggregationProjection(IOperator<TId> input, List<GroupingEvaluator<TId>> groupings, List<IAggregationEvaluator<TId>> aggregators, IRowColumn[] matchColumns)
      => BuildAggregateProjection(input, groupings, aggregators, matchColumns);

    IOperator<TId> IOperatorFactory<TId>.BuildOrderBy(IOperator<TId> input, IReadOnlyList<OrderByEvaluator<TId>> ordering)
      => BuildOrderBy(input, ordering);

    #endregion


    #region Reference implementation

    protected virtual IOperator<TId> BuildEmptyResult()
      => new EmptyResultReferenceOperator<TId>();

    protected virtual IOperator<TId> BuildExpandInto(IOperator<TId> input, VariableDefinition? sourceVariable, ExpandDirectionType? direction, string? relationLabel, VariableDefinition destinationVariable, string? destinationLabel, PropertyFilter? destinationPropertyFilter)
      => new ExpandIntoReferenceOperator<TId>(input, sourceVariable, direction, relationLabel, destinationVariable, destinationLabel, destinationPropertyFilter);

    protected virtual IOperator<TId> BuildCartesianProduct(IOperator<TId> left, IOperator<TId> right)
      => new CartesianProductReferenceOperator<TId>(left, right);


    protected virtual IOperator<TId> BuildFilter(IOperator<TId> input, RowEvaluator<TId> evaluator)
      => new FilterReferenceOperator<TId>(input, evaluator);


    protected virtual IOperator<TId> BuildLimit(IOperator<TId> input, int limit)
      => new LimitReferenceOperator<TId>(input, limit);


    protected virtual IOperator<TId> BuildProjection(IOperator<TId> input, IReadOnlyList<ProjectionEvaluator<TId>> projections)
      => new ProjectionReferenceOperator<TId>(input, projections);


    protected virtual IOperator<TId> BuildAggregateProjection(IOperator<TId> input, List<GroupingEvaluator<TId>> groupings, List<IAggregationEvaluator<TId>> aggregators, IRowColumn[] outputColumns)
      => new AggregateReferenceOperator<TId>(input, groupings, aggregators, NewRow, outputColumns);

    
    protected virtual IOperator<TId> BuildOrderBy(IOperator<TId> input, IReadOnlyList<OrderByEvaluator<TId>> ordering)
      => new OrderByReferenceOperator<TId>(input, ordering);

    #endregion


    #region Backend implementation specific

    protected abstract IRow<TId> NewRow(IRowColumn[] matchColumns);

    protected abstract IOperator<TId> BuildNodeScan(VariableDefinition variable, string? label, PropertyFilter? propertyFilter, IRowColumn[] matchColumns);

    protected abstract IOperator<TId> BuildExpandAll(IOperator<TId> input, VariableDefinition sourceVariable, ExpandDirectionType direction, string? relationLabel, VariableDefinition destinationVariable, string? destinationLabel, PropertyFilter? destinationPropertyFilter);


    #endregion
  }
}
