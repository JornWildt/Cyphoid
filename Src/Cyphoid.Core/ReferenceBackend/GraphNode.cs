using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public class GraphNode<TId> : IGraphNode where TId : IEquatable<TId>
  {
    public TId Id { get; private init; }

    public string Type { get; private init; }

    public IReadOnlyList<GraphEdge<TId>> Edges { get; private init; }

    public Dictionary<string, object?> Attributes { get; private init; }


    public GraphNode(
      TId id,
      string type,
      IReadOnlyList<GraphEdge<TId>> edges,
      Dictionary<string, object?> attributes)
    {
      Id = id;
      Type = type;
      Edges = edges;
      Attributes = attributes;
    }


    #region IGraphNode

    string IGraphNode.Type => Type;

    Dictionary<string, object?> IGraphNode.Attributes => Attributes;

    IReadOnlyList<IGraphEdge> IGraphNode.Edges => (IReadOnlyList<IGraphEdge>)Edges;

    bool IGraphNode.HasSameIdAs(IGraphNode? node)
    {
      if (node == null)
        return false;

      if (node is GraphNode<TId> n)
        return Id.Equals(n.Id);

      throw new NotImplementedException($"Cannot compare graph nodes of type {GetType()} with {node.GetType()}.");
    }

    #endregion
  }


  public class GraphEdge<TId> : IGraphEdge where TId : IEquatable<TId>
  {
    public string Type { get; private init; }
    
    public TId Target { get; private init;  }

    public GraphEdge(string type, TId target)
    {
      Type = type;
      Target = target;
    }

    string IGraphEdge.Type => Type;

    bool IGraphEdge.RefersTo(IGraphNode node)
    {
      if (node is GraphNode<TId> n)
        return Target.Equals(n.Id);

      throw new NotImplementedException($"Cannot compare graph edges of type {GetType()} to nodes of type {node.GetType()}.");
    }
  }
}
