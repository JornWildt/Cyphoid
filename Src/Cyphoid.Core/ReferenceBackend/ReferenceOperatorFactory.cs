using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public abstract class ReferenceOperatorFactory<TId> : IOperatorFactory<TId> where TId : IEquatable<TId>
  {
    #region IOperatorFactory

    IOperator<TId> IOperatorFactory<TId>.BuildEmptyResult()
      => BuildEmptyResult();

    IOperator<TId> IOperatorFactory<TId>.BuildNodeScan(VariableDefinition variable, string? label, PropertyFilter? propertyFilter)
      => BuildNodeScan(variable, label, propertyFilter);

    IOperator<TId> IOperatorFactory<TId>.BuildExpand(IOperator<TId> input, VariableDefinition sourceVariable, ExpandDirectionType direction, string? relationLabel, VariableDefinition destinationVariable, string? destinationLabel, PropertyFilter? destinationPropertyFilter)
      => BuildExpand(input, sourceVariable, direction, relationLabel, destinationVariable, destinationLabel, destinationPropertyFilter);

    IOperator<TId> IOperatorFactory<TId>.BuildFilter(IOperator<TId> input, RowEvaluator<TId> evaluator)
      => BuildFilter(input, evaluator);

    IOperator<TId> IOperatorFactory<TId>.BuildLimit(IOperator<TId> input, int limit)
      => BuildLimit(input, limit);

    IProjectionOperator IOperatorFactory<TId>.BuildProjection(IOperator<TId> input, IReadOnlyList<ProjectionEvaluator<TId>> projections)
      => BuildProjection(input, projections);

    #endregion


    #region Reference implementation

    protected virtual IOperator<TId> BuildEmptyResult()
      => new EmptyResultReferenceOperator<TId>();

    
    protected virtual IOperator<TId> BuildFilter(IOperator<TId> input, RowEvaluator<TId> evaluator)
      => new FilterReferenceOperator<TId>(input, evaluator);

    
    protected virtual IOperator<TId> BuildLimit(IOperator<TId> input, int limit)
      => new LimitReferenceOperator<TId>(input, limit);

    protected virtual IProjectionOperator BuildProjection(IOperator<TId> input, IReadOnlyList<ProjectionEvaluator<TId>> projections)
      => new ProjectionReferenceOperator<TId>(input, projections);

    #endregion


    #region Backend implementation specific

    protected abstract IOperator<TId> BuildNodeScan(VariableDefinition variable, string? label, PropertyFilter? propertyFilter);
    
    protected abstract IOperator<TId> BuildExpand(IOperator<TId> input, VariableDefinition sourceVariable, ExpandDirectionType direction, string? relationLabel, VariableDefinition destinationVariable, string? destinationLabel, PropertyFilter? destinationPropertyFilter);

    #endregion
  }
}
