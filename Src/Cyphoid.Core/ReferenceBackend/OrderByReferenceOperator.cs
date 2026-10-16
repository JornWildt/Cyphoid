using System.Runtime.CompilerServices;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.ReferenceBackend
{
  public record OrderByReferenceOperator(
    IOperator Input,
    IReadOnlyList<OrderByEvaluator> Ordering) : IOperator
  {
    async IAsyncEnumerable<IRow> IOperator.ExecuteAsync(IQueryContext context, [EnumeratorCancellation] CancellationToken ct)
    {
      ct.ThrowIfCancellationRequested();
      var result = Input.ExecuteAsync(context, ct);

      IOrderedAsyncEnumerable<IRow>? ordered = null;

      foreach (var orderBy in Ordering)
      {
        MixedValue Evaluate(IRow row)
        {
          ct.ThrowIfCancellationRequested();
          return orderBy.ExpressionEvaluator(row);
        }
        if (ordered is null)
        {
          ordered = orderBy.Direction == SortDirection.Ascending
              ? result.OrderBy(Evaluate)
              : result.OrderByDescending(Evaluate);
        }
        else
        {
          ordered = orderBy.Direction == SortDirection.Ascending
              ? ordered.ThenBy(Evaluate)
              : ordered.ThenByDescending(Evaluate);
        }
      }

      await foreach (var row in (ordered ?? result).WithCancellation(ct))
      {
        ct.ThrowIfCancellationRequested();
        yield return row;
      }
    }
  }
}
