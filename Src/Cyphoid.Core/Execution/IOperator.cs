namespace Cyphoid.Core.Execution
{
  public interface IOperator
  {
    IAsyncEnumerable<Row> ExecuteAsync(QueryContext context);
  }
}
