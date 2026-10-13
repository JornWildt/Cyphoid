using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public record OrderByReferenceOperator(
    IOperator Input,
    IReadOnlyList<OrderByEvaluator> Ordering) : IOperator
  {
    async IAsyncEnumerable<IRow> IOperator.ExecuteAsync(IQueryContext context)
    {
      var result = Input.ExecuteAsync(context);

      IOrderedAsyncEnumerable<IRow>? ordered = null;

      foreach (var orderBy in Ordering)
      {
        if (ordered is null)
        {
          ordered = orderBy.Direction == SortDirection.Ascending
              ? result.OrderBy(r => orderBy.ExpressionEvaluator(r))
              : result.OrderByDescending(r => orderBy.ExpressionEvaluator(r));
        }
        else
        {
          ordered = orderBy.Direction == SortDirection.Ascending
              ? ordered.ThenBy(r => orderBy.ExpressionEvaluator(r))
              : ordered.ThenByDescending(r => orderBy.ExpressionEvaluator(r));
        }
      }

      await foreach (var row in (ordered ?? result))
      {
        yield return row;
      }
    }
  }
}
