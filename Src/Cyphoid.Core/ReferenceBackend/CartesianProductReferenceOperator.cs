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
          var output = row1.Clone(row2.Columns);
          for (int i=0; i<row2.Values.Length; ++i)
          {
            if (row2.Values[i] != null)
              output.Values[i] = row2.Values[i];
          }
          yield return output;
        }
      }
    }
  }
}
