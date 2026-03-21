namespace Cyphoid.Core.Execution
{
  public record ProjectionEvaluator<TId>(RowEvaluator<TId> ExpressionEvaluator, string OutputName) where TId : IEquatable<TId>;
}
