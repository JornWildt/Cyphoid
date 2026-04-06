using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public record DistinctReferenceOperator<TId>(
    IOperator<TId> Input,
    IReadOnlyList<GroupingEvaluator<TId>> Groupings) : IOperator<TId> where TId : IEquatable<TId>
  {
    IAsyncEnumerable<IRow<TId>> IOperator<TId>.ExecuteAsync(IQueryContext context)
    {
      return Input.ExecuteAsync(context)
        .DistinctBy(r => new GroupKey<TId>(r, Groupings));
    }
  }
}
