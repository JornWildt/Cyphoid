namespace Cyphoid.Core.Execution
{
  public interface IOperator
  {
    /// <summary>Execute the operator, propagating ct to input operators and I/O.</summary>
    IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context, CancellationToken ct = default);
  }
}
