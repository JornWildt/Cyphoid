using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.ReferenceBackend
{
  public class Row : IRow
  {
    #region IRow

    IRowColumn[] IRow.Columns => Columns;

    MixedValue?[] IRow.Values => Values;

    IRow IRow.CopyAndExpand(IReadOnlyList<IRowColumn> outputColumns)
    {
      return CopyAndExpand(outputColumns);
    }

    IRow IRow.MergeWith(IRow other)
    {
      return MergeWith(other);
    }

    #endregion


    protected IRowColumn[] Columns { get; private init; }

    protected MixedValue?[] Values { get; private init; }

    public Row(IReadOnlyList<IRowColumn> columns)
    {
      Columns = columns.ToArray();
      Values = new MixedValue?[Columns.Length];
    }


    protected virtual IRow CopyAndExpand(IReadOnlyList<IRowColumn> outputColumns)
    {
      var clone = CreateEmptyRow(outputColumns);
      CopyMyContentToClone(clone);
      return clone;
    }


    protected virtual IRow MergeWith(IRow other)
    {
      var clone = CreateEmptyRow(other.Columns);
      CopyMyContentToClone(clone);
      CopyOtherContentToClone(other, clone);
      return clone;
    }

    protected virtual IRow CreateEmptyRow(IReadOnlyList<IRowColumn> outputColumns) => new Row(outputColumns);


    protected virtual void CopyMyContentToClone(IRow clone)
    {
      for (int i = 0; i < Values.Length; i++)
        clone.Values[i] = Values[i];
    }


    protected virtual void CopyOtherContentToClone(IRow source, IRow clone)
    {
      for (int i = 0; i < source.Values.Length; ++i)
      {
        if (source.Values[i] != null)
          clone.Values[i] = source.Values[i];
      }
    }
  }
}
