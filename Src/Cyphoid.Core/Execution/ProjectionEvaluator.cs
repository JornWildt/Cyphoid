namespace Cyphoid.Core.Execution
{
  public record ProjectionEvaluator<TId>(
    RowEvaluator<TId> ExpressionEvaluator, 
    VariableDefinition OutputVariable) where TId : IEquatable<TId>;
}
