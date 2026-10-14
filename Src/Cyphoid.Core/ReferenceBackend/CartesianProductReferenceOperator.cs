using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public record CartesianProductReferenceOperator(
    IOperator Left,
    IOperator Right) : IOperator
  {
    async IAsyncEnumerable<IRow> IOperator.ExecuteAsync(IQueryContext context)
    {
      await foreach (var row1 in Left.ExecuteAsync(context))
      {
        await foreach (var row2 in Right.ExecuteAsync(context))
        {
          var output = row1.MergeWith(row2);
          yield return output;
        }
      }
    }
  }
}
