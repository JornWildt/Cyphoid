namespace Cyphoid.Core.Execution
{
  public class Row
  {
    public GraphNode[] Nodes { get; private init; }

    public Row(int capacity)
    {
      Nodes = new GraphNode[capacity];
    }


    public Row Clone()
    {
      var clone = new Row(Nodes.Length);
      for (int i=0; i<Nodes.Length; i++)
        clone.Nodes[i] = Nodes[i];
      return clone;
    }
  }
}
