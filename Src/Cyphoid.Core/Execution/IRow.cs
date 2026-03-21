namespace Cyphoid.Core.Execution
{
  public interface IRow<TId> where TId : IEquatable<TId>
  {
    IGraphNode<TId>?[] Nodes { get; }
    IRow<TId> Clone();
  }
}
