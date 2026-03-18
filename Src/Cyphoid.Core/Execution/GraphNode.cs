namespace Cyphoid.Core.Execution
{
  public record GraphNode(
    string Id,
    Dictionary<string, string> Edges,
    Dictionary<string, object?> Attributes);
}
