using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.Execution
{
  public interface IRowColumn
  {
    int Index { get; }
    string Name { get; }
    MixedValue.ValueType Type { get; }
  }


  public interface IRow
  {
    IRowColumn[] Columns { get; }
    MixedValue?[] Values { get; }
    IRow CopyAndExpand(IReadOnlyList<IRowColumn> outputColumns);
    IRow MergeWith(IRow other);
  }
}
