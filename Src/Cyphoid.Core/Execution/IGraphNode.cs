namespace Cyphoid.Core.Execution
{
  public interface IGraphNode
  {
    string Id { get; }
    Dictionary<string, string> Edges { get; }
    Dictionary<string, object?> Attributes { get; }
  }
}
