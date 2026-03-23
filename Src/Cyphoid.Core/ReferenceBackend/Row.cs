using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.ReferenceBackend
{
  public class Row<TId> : IRow<TId> where TId : IEquatable<TId>
  {
    #region IRow

    MixedValue?[] IRow<TId>.Variables => Variables;

    IRow<TId> IRow<TId>.Clone()
    {
      return Clone();
    }

    #endregion


    protected MixedValue?[] Variables { get; private init; }

    public Row(int capacity)
    {
      Variables = new MixedValue?[capacity];
    }


    protected Row<TId> Clone()
    {
      var clone = new Row<TId>(Variables.Length);
      for (int i=0; i< Variables.Length; i++)
        clone.Variables[i] = Variables[i];
      return clone;
    }
  }
}
