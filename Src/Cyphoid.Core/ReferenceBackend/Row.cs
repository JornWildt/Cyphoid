using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public class Row<TId> : IRow<TId> where TId : IEquatable<TId>
  {
    #region IRow

    IGraphNode<TId>[] IRow<TId>.Nodes => Nodes;

    IRow<TId> IRow<TId>.Clone()
    {
      return Clone();
    }

    #endregion


    protected IGraphNode<TId>[] Nodes { get; private init; }

    public Row(int capacity)
    {
      Nodes = new IGraphNode<TId>[capacity];
    }


    protected Row<TId> Clone()
    {
      var clone = new Row<TId>(Nodes.Length);
      for (int i=0; i<Nodes.Length; i++)
        clone.Nodes[i] = Nodes[i];
      return clone;
    }
  }
}
