using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public abstract class ReferenceOperatorFactory : IOperatorFactory
  {
    #region IOperatorFactory

    IRow IOperatorFactory.NewRow(IRowColumn[] matchColumns)
      => NewRow(matchColumns);

    IOperator IOperatorFactory.BuildEmptyResult()
      => BuildEmptyResult();

    IOperator IOperatorFactory.BuildNodeScan(VariableDefinition variable, string? label, PropertyFilter? propertyFilter, string? targetRelationshipLabel, IRowColumn[] matchColumns)
      => BuildNodeScan(variable, label, propertyFilter, targetRelationshipLabel, matchColumns);

    IOperator IOperatorFactory.BuildExpandAll(IOperator input, bool optional, VariableDefinition sourceVariable, ExpandDirectionType direction, string? relationLabel, VariableDefinition destinationVariable, string? destinationLabel, PropertyFilter? destinationPropertyFilter, IRowColumn[] matchColumns)
      => BuildExpandAll(input, optional, sourceVariable, direction, relationLabel, destinationVariable, destinationLabel, destinationPropertyFilter, matchColumns);

    IOperator IOperatorFactory.BuildExpandInto(IOperator input, bool optional, VariableDefinition? sourceVariable, ExpandDirectionType? direction, string? relationLabel, VariableDefinition destinationVariable, string? destinationLabel, PropertyFilter? destinationPropertyFilter)
      => BuildExpandInto(input, optional, sourceVariable, direction, relationLabel, destinationVariable, destinationLabel, destinationPropertyFilter);

    IOperator IOperatorFactory.BuildCartesianProduct(IOperator left, IOperator right)
      => BuildCartesianProduct(left, right);

    IOperator IOperatorFactory.BuildFilter(IOperator input, RowEvaluator evaluator)
      => BuildFilter(input, evaluator);

    IOperator IOperatorFactory.BuildLimit(IOperator input, int limit)
      => BuildLimit(input, limit);

    IOperator IOperatorFactory.BuildProjection(IOperator input, IReadOnlyList<ProjectionEvaluator> projections)
      => BuildProjection(input, projections);

    IOperator IOperatorFactory.BuildDistinct(IOperator input, List<GroupingEvaluator> groupings)
      => BuildDistinct(input, groupings);

    IOperator IOperatorFactory.BuildAggregationProjection(IOperator input, List<GroupingEvaluator> groupings, List<IAggregationEvaluator> aggregators, IRowColumn[] matchColumns)
      => BuildAggregateProjection(input, groupings, aggregators, matchColumns);

    IOperator IOperatorFactory.BuildOrderBy(IOperator input, IReadOnlyList<OrderByEvaluator> ordering)
      => BuildOrderBy(input, ordering);

    #endregion


    #region Reference implementation

    protected virtual IOperator BuildEmptyResult()
      => new EmptyResultReferenceOperator();

    protected virtual IOperator BuildExpandInto(IOperator input, bool optional, VariableDefinition? sourceVariable, ExpandDirectionType? direction, string? relationLabel, VariableDefinition destinationVariable, string? destinationLabel, PropertyFilter? destinationPropertyFilter)
      => new ExpandIntoReferenceOperator(input, optional, sourceVariable, direction, relationLabel, destinationVariable, destinationLabel, destinationPropertyFilter);

    protected virtual IOperator BuildCartesianProduct(IOperator left, IOperator right)
      => new CartesianProductReferenceOperator(left, right);


    protected virtual IOperator BuildFilter(IOperator input, RowEvaluator evaluator)
      => new FilterReferenceOperator(input, evaluator);


    protected virtual IOperator BuildLimit(IOperator input, int limit)
      => new LimitReferenceOperator(input, limit);


    protected virtual IOperator BuildProjection(IOperator input, IReadOnlyList<ProjectionEvaluator> projections)
      => new ProjectionReferenceOperator(input, projections);


    protected virtual IOperator BuildDistinct(IOperator input, List<GroupingEvaluator> groupings)
      => new DistinctReferenceOperator(input, groupings);


    protected virtual IOperator BuildAggregateProjection(IOperator input, List<GroupingEvaluator> groupings, List<IAggregationEvaluator> aggregators, IRowColumn[] outputColumns)
      => new AggregateReferenceOperator(input, groupings, aggregators, NewRow, outputColumns);


    protected virtual IOperator BuildOrderBy(IOperator input, IReadOnlyList<OrderByEvaluator> ordering)
      => new OrderByReferenceOperator(input, ordering);

    #endregion


    #region Backend implementation specific

    protected abstract IRow NewRow(IRowColumn[] matchColumns);

    protected abstract IOperator BuildNodeScan(VariableDefinition variable, string? label, PropertyFilter? propertyFilter, string? targetRelationshipLabel, IRowColumn[] matchColumns);

    protected abstract IOperator BuildExpandAll(IOperator input, bool optional, VariableDefinition sourceVariable, ExpandDirectionType direction, string? relationLabel, VariableDefinition destinationVariable, string? destinationLabel, PropertyFilter? destinationPropertyFilter, IRowColumn[] matchColumns);


    #endregion
  }
}
