using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public class FilterReferenceOperator<TId> : IOperator<TId> where TId : IEquatable<TId>
  {
    IOperator<TId> Input;
    RowEvaluator<TId> PredicateEvaluator;

    
    public FilterReferenceOperator(
      IOperator<TId> input,
      RowEvaluator<TId> predicateEvaluator)
    {
      Input = input;
      PredicateEvaluator = predicateEvaluator;
    }


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
