using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.ReferenceBackend
{
  public class OrderByReferenceOperator<TId> : IOperator<TId> where TId : IEquatable<TId>
  {
    IOperator<TId> Input;
    IReadOnlyList<OrderByEvaluator<TId>> Ordering;


    public OrderByReferenceOperator(IOperator<TId> input, IReadOnlyList<OrderByEvaluator<TId>> ordering)
    {
      Input = input;
      Ordering = ordering;
    }

    async IAsyncEnumerable<IRow<TId>> IOperator<TId>.ExecuteAsync(IQueryContext context)
    {
      var result = Input.ExecuteAsync(context);

      IOrderedAsyncEnumerable<IRow<TId>>? ordered = null;

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
