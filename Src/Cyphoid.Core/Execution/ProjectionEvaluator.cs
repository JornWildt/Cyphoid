using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.Execution
{
  public record ProjectionEvaluator(Func<Row, MixedValue> ExpressionEvaluator, string OutputName);
}
