using Cyphoid.Core.Execution;

namespace Cyphoid.Core.ReferenceBackend
{
  public record GraphNode(
    string Id,
    Dictionary<string, string> Edges,
    Dictionary<string, object?> Attributes) : IGraphNode;
}
