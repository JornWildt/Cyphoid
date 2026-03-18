namespace Cyphoid.Core.Execution
{
  public record GraphNode(
    Dictionary<string, string> Edges,
    Dictionary<string, object?> Attributes);
}
