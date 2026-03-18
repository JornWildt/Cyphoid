using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Tests.TestBackend.Operators
{
  internal class FilterOperator : OperatorBase, IOperator
  {
    IOperator Input;
    Func<Row, MixedValue> PredicateEvaluator;

    public FilterOperator(
      InMemoryGraph graph,
      IOperator input,
      Func<Row, MixedValue> predicateEvaluator)
      : base(graph)
    {
      Input = input;
      PredicateEvaluator = predicateEvaluator;
    }


    async IAsyncEnumerable<Row> IOperator.ExecuteAsync(QueryContext context)
    {
      await foreach(var row in Input.ExecuteAsync(context))
      {
        var ok = PredicateEvaluator(row);
        if (!ok.IsAnythingButTrue())
          yield return row;
      }
    }
  }
}
