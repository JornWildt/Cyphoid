using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.ReferenceBackend
{
  public class Row<TId> : IRow<TId> where TId : IEquatable<TId>
  {
    #region IRow

    IRowColumn[] IRow<TId>.Columns => Columns;

    MixedValue?[] IRow<TId>.Values => Values;

    IRow<TId> IRow<TId>.Clone(IReadOnlyList<IRowColumn> outputColumns)
    {
      return Clone(outputColumns);
    }

    #endregion


    protected IRowColumn[] Columns { get; private init; }

    protected MixedValue?[] Values { get; private init; }

    public Row(IReadOnlyList<IRowColumn> columns)
    {
      Columns = columns.ToArray();
      Values = new MixedValue?[Columns.Length];
    }


    protected Row<TId> Clone(IReadOnlyList<IRowColumn> outputColumns)
    {
      var clone = new Row<TId>(outputColumns);
      for (int i=0; i< Values.Length; i++)
        clone.Values[i] = Values[i];
      return clone;
    }
  }
}
