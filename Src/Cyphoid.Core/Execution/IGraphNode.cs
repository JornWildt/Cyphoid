namespace Cyphoid.Core.Execution
{
  public interface IGraphNode
  {
    string Type { get; }
    Dictionary<string, object?> Attributes { get; }
    IReadOnlyList<IGraphEdge> Edges { get; }
    bool HasSameIdAs(IGraphNode? other);
  }


  public interface IGraphEdge
  {
    string Type { get; }
    bool RefersTo(IGraphNode node);
  }
}
