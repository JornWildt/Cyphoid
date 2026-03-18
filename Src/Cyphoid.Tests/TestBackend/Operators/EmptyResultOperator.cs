using Cyphoid.Core.Execution;

namespace Cyphoid.Tests.TestBackend.Operators
{
  internal class EmptyResultOperator : IOperator
  {
    async IAsyncEnumerable<Row> IOperator.ExecuteAsync(QueryContext context)
    {
      yield return new Row(0);
    }
  }
}
