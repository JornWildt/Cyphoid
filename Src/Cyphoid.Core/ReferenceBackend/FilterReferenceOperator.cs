using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public class FilterReferenceOperator : IOperator
  {
    IOperator Input;
    RowEvaluator PredicateEvaluator;

    
    public FilterReferenceOperator(
      IOperator input,
      RowEvaluator predicateEvaluator)
    {
      Input = input;
      PredicateEvaluator = predicateEvaluator;
    }


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
