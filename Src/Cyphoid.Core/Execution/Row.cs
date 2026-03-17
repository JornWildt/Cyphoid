namespace Cyphoid.Core.Execution
{
  public class Row
  {
    public GraphNode[] Nodes { get; private init; }

    public Row(int capacity)
    {
      Nodes = new GraphNode[capacity];
    }
  }
}
