using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.ReferenceBackend
{
  public class Row<TId> : IRow<TId> where TId : IEquatable<TId>
  {
    #region IRow

    IRowColumn[] IRow<TId>.Columns => Columns;

    MixedValue?[] IRow<TId>.Values => Values;

    IRow<TId> IRow<TId>.Clone()
    {
      return Clone();
    }

    #endregion


    protected IRowColumn[] Columns { get; private init; }

    protected MixedValue?[] Values { get; private init; }

    public Row(IRowColumn[] columns)
    {
      Columns = columns;
      Values = new MixedValue?[columns.Length];
    }


    protected Row<TId> Clone()
    {
      var clone = new Row<TId>(Columns);
      for (int i=0; i< Values.Length; i++)
        clone.Values[i] = Values[i];
      return clone;
    }
  }
}
