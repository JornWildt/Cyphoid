namespace Cyphoid.Core.Execution
{
  public interface IProjectionOperator
  {
    IAsyncEnumerable<IDictionary<string, object?>> ExecuteAsync(IQueryContext context);
  }
}
