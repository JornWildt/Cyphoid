using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.Execution
{
  // FIXME: Remove <TId> (it is found in MixedValue instead)
  public interface IRow<TId> where TId : IEquatable<TId>
  {
    MixedValue?[] Variables { get; }
    IRow<TId> Clone();
  }
}
