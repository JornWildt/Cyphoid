using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cyphoid.Tests.TestBackend
{
  using System;
  using System.Collections.Generic;
  using System.Linq;

  public class InMemoryGraph
  {
    private readonly Dictionary<string, Node> _nodes = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, Node> Nodes => _nodes;

    public Node AddNode(string id, params string[] labels)
    {
      if (string.IsNullOrWhiteSpace(id))
        throw new ArgumentException("Node id cannot be null or empty.", nameof(id));

      if (_nodes.TryGetValue(id, out var existing))
      {
        foreach (var label in labels ?? Array.Empty<string>())
        {
          if (!string.IsNullOrWhiteSpace(label))
            existing.Labels.Add(label);
        }

        return existing;
      }

      var node = new Node(id);

      foreach (var label in labels ?? Array.Empty<string>())
      {
        if (!string.IsNullOrWhiteSpace(label))
          node.Labels.Add(label);
      }

      _nodes[id] = node;
      return node;
    }

    public bool TryGetNode(string id, out Node node)
    {
      return _nodes.TryGetValue(id, out node!);
    }

    public Node GetNode(string id)
    {
      if (!_nodes.TryGetValue(id, out var node))
        throw new KeyNotFoundException($"Node '{id}' was not found.");

      return node;
    }

    public void RemoveNode(string id)
    {
      if (!_nodes.TryGetValue(id, out var node))
        return;

      foreach (var edge in node.Outgoing.ToList())
      {
        edge.To.Incoming.Remove(edge);
      }

      foreach (var edge in node.Incoming.ToList())
      {
        edge.From.Outgoing.Remove(edge);
      }

      _nodes.Remove(id);
    }

    public Node SetNodeProperty(string nodeId, string key, object? value)
    {
      var node = GetNode(nodeId);
      node.Properties[key] = value;
      return node;
    }

    public Edge AddEdge(string fromId, string toId, string type)
    {
      if (string.IsNullOrWhiteSpace(type))
        throw new ArgumentException("Edge type cannot be null or empty.", nameof(type));

      var from = GetNode(fromId);
      var to = GetNode(toId);

      var edge = new Edge(from, to, type);
      from.Outgoing.Add(edge);
      to.Incoming.Add(edge);

      return edge;
    }

    public Edge AddEdge(string fromId, string toId, string type, Dictionary<string, object?> properties)
    {
      var edge = AddEdge(fromId, toId, type);

      foreach (var kvp in properties)
        edge.Properties[kvp.Key] = kvp.Value;

      return edge;
    }

    public Edge SetEdgeProperty(Edge edge, string key, object? value)
    {
      edge.Properties[key] = value;
      return edge;
    }

    public IEnumerable<Edge> Outgoing(string nodeId, string? type = null)
    {
      var node = GetNode(nodeId);
      return type == null
          ? node.Outgoing
          : node.Outgoing.Where(e => string.Equals(e.Type, type, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<Edge> Incoming(string nodeId, string? type = null)
    {
      var node = GetNode(nodeId);
      return type == null
          ? node.Incoming
          : node.Incoming.Where(e => string.Equals(e.Type, type, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<Node> Neighbors(string nodeId, string? edgeType = null)
    {
      return Outgoing(nodeId, edgeType).Select(e => e.To);
    }

    public IEnumerable<Node> NodesWithLabel(string label)
    {
      return _nodes.Values.Where(n => n.Labels.Contains(label));
    }

    public sealed class Node
    {
      internal Node(string id)
      {
        Id = id;
      }

      public string Id { get; }
      public HashSet<string> Labels { get; } = new(StringComparer.OrdinalIgnoreCase);
      public Dictionary<string, object?> Properties { get; } = new(StringComparer.OrdinalIgnoreCase);

      internal List<Edge> Outgoing { get; } = new();
      internal List<Edge> Incoming { get; } = new();

      public override string ToString()
      {
        var labels = Labels.Count == 0 ? "" : ":" + string.Join(":", Labels);
        return $"({Id}{labels})";
      }
    }

    public sealed class Edge
    {
      internal Edge(Node from, Node to, string type)
      {
        From = from;
        To = to;
        Type = type;
      }

      public Node From { get; }
      public Node To { get; }
      public string Type { get; }
      public Dictionary<string, object?> Properties { get; } = new(StringComparer.OrdinalIgnoreCase);

      public override string ToString()
      {
        return $"({From.Id})-[:{Type}]->({To.Id})";
      }
    }
  }
}
