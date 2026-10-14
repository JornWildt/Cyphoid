using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public record OptionalMatchReferenceOperator(
    IOperator Input, Func<IRow, IOperator> BuildMatch, IRowColumn[] Columns) : IOperator
  {
    public async IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context)
    {
      await foreach (var row in Input.ExecuteAsync(context))
      {
        bool matched = false;
        await foreach (var result in BuildMatch(row).ExecuteAsync(context))
        {
          matched = true;
          yield return result;
        }
        if (!matched)
          yield return row.CopyAndExpand(Columns);
      }
    }
  }
}
