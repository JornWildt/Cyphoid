namespace Cyphoid.Core.Execution
{
  public interface IGraphNode
  {
    string Type { get; }
    Dictionary<string, object?> Attributes { get; }
    bool HasSameIdAs(IGraphNode? other);
  }


  public interface IGraphNode<TId> : IGraphNode where TId : IEquatable<TId>
  {
    TId Id { get; }
    IReadOnlyList<IGraphEdge<TId>> Edges { get; }
  }

  public interface IGraphEdge<TId> where TId : IEquatable<TId>
  {
    string Type { get; }
    TId Target { get; }
  }
}
