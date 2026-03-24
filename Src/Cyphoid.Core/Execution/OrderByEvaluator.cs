namespace Cyphoid.Core.Execution
{
  public enum SortDirection { Ascending, Descending}

  public record OrderByEvaluator<TId>(
    RowEvaluator<TId> ExpressionEvaluator,
    SortDirection Direction) where TId : IEquatable<TId>;
}
