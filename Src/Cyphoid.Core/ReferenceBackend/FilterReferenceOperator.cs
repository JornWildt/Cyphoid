using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public record FilterReferenceOperator<TId>(
    IOperator<TId> Input,
    RowEvaluator<TId> PredicateEvaluator) : IOperator<TId> where TId : IEquatable<TId>
  {
    async IAsyncEnumerable<IRow<TId>> IOperator<TId>.ExecuteAsync(IQueryContext context)
    {
      await foreach (var row in Input.ExecuteAsync(context))
      {
        var ok = PredicateEvaluator(row);
        if (!ok.IsAnythingButTrue())
          yield return row;
      }
    }
  }
}
