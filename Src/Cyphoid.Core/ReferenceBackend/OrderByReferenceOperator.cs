using System.Runtime.CompilerServices;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.ReferenceBackend
{
  public record OrderByReferenceOperator(
    IOperator Input,
    IReadOnlyList<OrderByEvaluator> Ordering) : IOperator
  {
    private static readonly IComparer<MixedValue> BooleanOrdering = Comparer<MixedValue>.Create((left, right) =>
      left.TryGetBool(out var a) && right.TryGetBool(out var b) ? a.CompareTo(b)
        : Comparer<MixedValue>.Default.Compare(left, right));
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
              ? result.OrderBy(Evaluate, BooleanOrdering)
              : result.OrderByDescending(Evaluate, BooleanOrdering);
        }
        else
        {
          ordered = orderBy.Direction == SortDirection.Ascending
              ? ordered.ThenBy(Evaluate, BooleanOrdering)
              : ordered.ThenByDescending(Evaluate, BooleanOrdering);
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
