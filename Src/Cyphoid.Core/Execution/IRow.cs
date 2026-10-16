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
    RowSchema Schema { get; }
    IReadOnlyList<IRowColumn> Columns => Schema.Columns;
    MixedValue?[] Values { get; }
    IRow CopyAndExpand(RowSchema outputColumns);
    IRow MergeWith(IRow other);
  }
}
