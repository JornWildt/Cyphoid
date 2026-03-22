using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public record CartesianProductReferenceOperator<TId>(
    IOperator<TId> Left,
    IOperator<TId> Right) : IOperator<TId> where TId : IEquatable<TId>
  {
    async IAsyncEnumerable<IRow<TId>> IOperator<TId>.ExecuteAsync(IQueryContext context)
    {
      await foreach (var row1 in Left.ExecuteAsync(context))
      {
        await foreach (var row2 in Right.ExecuteAsync(context))
        {
          var output = row1.Clone();
          for (int i=0; i<row2.Nodes.Length; ++i)
          {
            if (row2.Nodes[i] != null)
              output.Nodes[i] = row2.Nodes[i];
          }
          yield return output;
        }
      }
    }
  }
}
