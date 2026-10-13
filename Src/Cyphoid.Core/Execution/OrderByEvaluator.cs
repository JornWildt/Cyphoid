namespace Cyphoid.Core.Execution
{
  public enum SortDirection { Ascending, Descending}

  public record OrderByEvaluator(
    RowEvaluator ExpressionEvaluator,
    SortDirection Direction);
}
