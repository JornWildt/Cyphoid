using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.ReferenceBackend
{
  public class Row : IRow
  {
    #region IRow

    IRowColumn[] IRow.Columns => Columns;

    MixedValue?[] IRow.Values => Values;

    IRow IRow.Clone(IReadOnlyList<IRowColumn> outputColumns)
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


    protected Row Clone(IReadOnlyList<IRowColumn> outputColumns)
    {
      var clone = new Row(outputColumns);
      for (int i=0; i<Values.Length; i++)
        clone.Values[i] = Values[i];
      return clone;
    }
  }
}
