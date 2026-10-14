namespace Cyphoid.Core.Execution
{
  using Cyphoid.Core.Expressions;

  public interface IGraphNode
  {
    /// <summary>Returns the backend node identifier as a query value.</summary>
    MixedValue GetId();
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
