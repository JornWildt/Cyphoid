using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public class Row : IRow
  {
    #region IRow

    IReadOnlyList<IGraphNode> IRow.Nodes => Nodes;

    IRow IRow.Clone()
    {
      return Clone();
    }

    #endregion


    protected IGraphNode[] Nodes { get; private init; }

    public Row(int capacity)
    {
      Nodes = new IGraphNode[capacity];
    }


    protected Row Clone()
    {
      var clone = new Row(Nodes.Length);
      for (int i=0; i<Nodes.Length; i++)
        clone.Nodes[i] = Nodes[i];
      return clone;
    }
  }
}
