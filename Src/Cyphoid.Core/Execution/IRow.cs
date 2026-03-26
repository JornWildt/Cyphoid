using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.Execution
{
  public interface IRowColumn
  {
    int Index { get; }
    string Name { get; }
    MixedValue.ValueType Type { get; }
  }


  // FIXME: Remove <TId> (it is found in MixedValue instead)
  public interface IRow<TId> where TId : IEquatable<TId>
  {
    IRowColumn[] Columns { get; }
    MixedValue?[] Values { get; }
    IRow<TId> Clone(IReadOnlyList<IRowColumn> outputColumns);
  }
}
