using System.Text;
using Cyphoid.Core.Exceptions;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.SyntaxTree;

public sealed record MapEntryNode(string Key, ExprNode Value);

public sealed record MapLiteralNode(IReadOnlyList<MapEntryNode> Entries)
  : ExprNode(Entries.All(e => e.Value.ValueKind == ValueKindType.Const) ? ValueKindType.Const : ValueKindType.Variable)
{
  public override RowEvaluator BuildEvaluator()
  {
    var entries = Entries.Select(e => (e.Key, Evaluate: e.Value.CompileEvaluator())).ToArray();
    return row => MixedValue.Map(entries.Select(e => new KeyValuePair<string, MixedValue>(e.Key, e.Evaluate(row))));
  }

  public override void PrettyPrint(StringBuilder sb)
  {
    sb.Append('{');
    for (var i = 0; i < Entries.Count; i++)
    {
      if (i > 0) sb.Append(", ");
      sb.Append(Entries[i].Key).Append(": ");
      Entries[i].Value.PrettyPrint(sb);
    }
    sb.Append('}');
  }
}

public sealed record MapAccessNode(ExprNode Target, ExprNode Key, string? Property = null)
  : ExprNode(ValueKindType.Variable)
{
  public override RowEvaluator BuildEvaluator()
  {
    var target = Target.CompileEvaluator();
    var key = Key.CompileEvaluator();
    return row => Read(target(row), key(row));
  }

  internal static MixedValue Read(MixedValue target, MixedValue key)
  {
    if (target.IsNull() || key.IsNull()) return MixedValue.Null();
    var name = key.AsString();
    if (target.TryGetMap(out var map))
      return map.TryGetValue(name, out var value) ? value : MixedValue.Null();
    if (target.TryGetGraphNode(out var node))
      return node.Attributes.TryGetValue(name, out var attribute) ? MixedValue.FromObject(attribute) : MixedValue.Null();
    throw new RuntimeException($"Property lookup requires a map or node; got {target.Type}.");
  }

  public override void PrettyPrint(StringBuilder sb)
  {
    Target.PrettyPrint(sb);
    if (Property != null) sb.Append('.').Append(Property);
    else
    {
      sb.Append('[');
      Key.PrettyPrint(sb);
      sb.Append(']');
    }
  }
}

public sealed record MapProjectionEntry(string? Key, ExprNode? Value, bool AllProperties = false,
  bool Selector = false, bool VariableSelector = false);

public sealed record MapProjectionNode(ExprNode Target, IReadOnlyList<MapProjectionEntry> Entries)
  : ExprNode(ValueKindType.Variable)
{
  public override RowEvaluator BuildEvaluator()
  {
    var target = Target.CompileEvaluator();
    var entries = Entries.Select(e => (Entry: e, Evaluate: e.Value?.CompileEvaluator())).ToArray();
    return row =>
    {
      var source = target(row);
      if (source.IsNull()) return MixedValue.Null();
      if (source.Type is not (MixedValue.ValueType.Map or MixedValue.ValueType.Node))
        throw new RuntimeException($"Map projection requires a map or node; got {source.Type}.");
      var values = new Dictionary<string, MixedValue>(StringComparer.Ordinal);
      // Copy-all provides defaults; explicit entries take precedence irrespective of position.
      if (Entries.Any(e => e.AllProperties))
      {
        if (source.TryGetMap(out var map))
          foreach (var pair in map) values[pair.Key] = pair.Value;
        else
          foreach (var pair in source.AsGraphNode().Attributes) values[pair.Key] = MixedValue.FromObject(pair.Value);
      }
      foreach (var entry in entries)
      {
        if (entry.Entry.AllProperties) continue;
        values[entry.Entry.Key!] = entry.Entry.Selector
          ? MapAccessNode.Read(source, MixedValue.String(entry.Entry.Key)) : entry.Evaluate!(row);
      }
      return MixedValue.Map(values);
    };
  }

  public override void PrettyPrint(StringBuilder sb)
  {
    Target.PrettyPrint(sb);
    sb.Append(" {");
    for (var i = 0; i < Entries.Count; i++)
    {
      if (i > 0) sb.Append(", ");
      var entry = Entries[i];
      if (entry.AllProperties) sb.Append(".*");
      else if (entry.Selector) sb.Append('.').Append(entry.Key);
      else if (entry.VariableSelector) entry.Value!.PrettyPrint(sb);
      else
      {
        sb.Append(entry.Key).Append(": ");
        entry.Value!.PrettyPrint(sb);
      }
    }
    sb.Append('}');
  }
}
