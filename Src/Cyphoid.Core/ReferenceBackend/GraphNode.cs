using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public record GraphNode<TId>(
    TId Id,
    string Type,
    IReadOnlyList<IGraphEdge<TId>> Edges,
    Dictionary<string, object?> Attributes) : IGraphNode<TId> where TId : IEquatable<TId>
  {
    bool IGraphNode.HasSameIdAs(IGraphNode? other)
    {
      return (other is IGraphNode<TId> otherNode && Id.Equals(otherNode.Id));
    }
  }


  public record GraphEdge<TId>(
    string Type,
    TId Target) : IGraphEdge<TId> where TId : IEquatable<TId>;
}
