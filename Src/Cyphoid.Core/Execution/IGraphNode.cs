namespace Cyphoid.Core.Execution
{
  public interface IGraphNode
  {
    string Type { get; }
    Dictionary<string, object?> Attributes { get; }
  }


  public interface IGraphNode<TId> : IGraphNode where TId : IEquatable<TId>
  {
    TId Id { get; }
    Dictionary<string, TId> Edges { get; }
  }
}
