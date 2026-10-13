using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public record DistinctReferenceOperator(
    IOperator Input,
    IReadOnlyList<GroupingEvaluator> Groupings) : IOperator
  {
    IAsyncEnumerable<IRow> IOperator.ExecuteAsync(IQueryContext context)
    {
      return Input.ExecuteAsync(context)
        .DistinctBy(r => new GroupKey(r, Groupings));
    }
  }
}
