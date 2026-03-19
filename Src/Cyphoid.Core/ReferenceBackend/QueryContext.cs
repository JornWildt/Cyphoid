using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public record QueryContext(int RowSize) : IQueryContext;
}
