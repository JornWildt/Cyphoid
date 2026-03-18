namespace Cyphoid.Core.Execution
{
  public interface IProjectionOperator
  {
    // FIXME: object or MixedValue???
    IAsyncEnumerable<Dictionary<string,object?>> ExecuteAsync(QueryContext context);
  }
}
