namespace Cyphoid.Core.Execution
{
  public record ProjectionEvaluator(RowEvaluator ExpressionEvaluator, string OutputName);
}
