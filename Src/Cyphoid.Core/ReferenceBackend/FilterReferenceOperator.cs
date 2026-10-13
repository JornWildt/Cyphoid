using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public record FilterReferenceOperator(
    IOperator Input,
    RowEvaluator PredicateEvaluator) : IOperator
  {
    async IAsyncEnumerable<IRow> IOperator.ExecuteAsync(IQueryContext context)
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
