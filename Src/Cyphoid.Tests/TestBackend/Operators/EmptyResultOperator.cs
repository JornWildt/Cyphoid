using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;

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
