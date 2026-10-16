using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.ReferenceBackend
{
  public class Row : IRow
  {
    #region IRow

    IReadOnlyList<IRowColumn> IRow.Columns => Schema.Columns;

    MixedValue?[] IRow.Values => Values;

    IRow IRow.CopyAndExpand(RowSchema outputColumns)
    {
      return CopyAndExpand(outputColumns);
    }

    IRow IRow.MergeWith(IRow other)
    {
      return MergeWith(other);
    }

    #endregion


    public RowSchema Schema { get; }
    protected IReadOnlyList<IRowColumn> Columns => Schema.Columns;

    protected MixedValue?[] Values { get; private init; }

    public Row(RowSchema columns)
    {
      ArgumentNullException.ThrowIfNull(columns);
      Schema = columns;
      Values = new MixedValue?[Schema.Count];
    }


    protected virtual IRow CopyAndExpand(RowSchema outputColumns)
    {
      var clone = CreateEmptyRow(outputColumns);
      CopyMyContentToClone(clone);
      return clone;
    }


    protected virtual IRow MergeWith(IRow other)
    {
      var clone = CreateEmptyRow(other.Schema);
      CopyMyContentToClone(clone);
      CopyOtherContentToClone(other, clone);
      return clone;
    }

    protected virtual IRow CreateEmptyRow(RowSchema outputColumns) => new Row(outputColumns);


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
