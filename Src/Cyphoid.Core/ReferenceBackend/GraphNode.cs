using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public record GraphNode<TId>(
    TId Id,
    string Type,
    Dictionary<string, TId> Edges,
    Dictionary<string, object?> Attributes) : IGraphNode<TId> where TId : IEquatable<TId>
  {
    bool IGraphNode.HasSameIdAs(IGraphNode? other)
    {
      return (other is IGraphNode<TId> otherNode && Id.Equals(otherNode.Id));
    }
  }
}
