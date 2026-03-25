using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.ReferenceBackend.Aggregation
{
  public interface IAggregateState<TId> where TId : IEquatable<TId>
  {
    void Add(IRow<TId> row);
    MixedValue? GetResult();
  }
}
