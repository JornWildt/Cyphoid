using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public abstract class ReferenceOperatorFactory : IOperatorFactory
  {
    #region IOperatorFactory

    IOperator IOperatorFactory.BuildEmptyResult()
      => BuildEmptyResult();

    IOperator IOperatorFactory.BuildExpand(IOperator input, VariableDefinition sourceVariable, ExpandDirectionType direction, string? relationLabel, VariableDefinition destinationVariable, string? destinationLabel, PropertyFilter? destinationPropertyFilter)
      => BuildExpand(input, sourceVariable, direction, relationLabel, destinationVariable, destinationLabel, destinationPropertyFilter);

    IOperator IOperatorFactory.BuildFilter(IOperator input, RowEvaluator evaluator)
      => BuildFilter(input, evaluator);

    IOperator IOperatorFactory.BuildLimit(IOperator input, int limit)
      => BuildLimit(input, limit);

    IOperator IOperatorFactory.BuildNodeScan(VariableDefinition variable, string? label, PropertyFilter? propertyFilter)
      => BuildNodeScan(variable, label, propertyFilter);

    IProjectionOperator IOperatorFactory.BuildProjection(IOperator input, IReadOnlyList<ProjectionEvaluator> projections)
      => BuildProjection(input, projections);

    #endregion


    #region Reference implementation

    protected virtual IOperator BuildEmptyResult()
      => new EmptyResultReferenceOperator();

    protected virtual IOperator BuildFilter(IOperator input, RowEvaluator evaluator)
      => new FilterReferenceOperator(Graph, input, evaluator);

    protected abstract IOperator BuildLimit(IOperator input, int limit);
    protected abstract IProjectionOperator BuildProjection(IOperator input, IReadOnlyList<ProjectionEvaluator> projections);

    #endregion

    protected abstract IOperator BuildNodeScan(VariableDefinition variable, string? label, PropertyFilter? propertyFilter);
    
    protected abstract IOperator BuildExpand(IOperator input, VariableDefinition sourceVariable, ExpandDirectionType direction, string? relationLabel, VariableDefinition destinationVariable, string? destinationLabel, PropertyFilter? destinationPropertyFilter);
  }
}
