using System.Collections;
using Cyphoid.Core.Exceptions;
using Cyphoid.Core.Expressions;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Execution;

/// <summary>An immutable ordinal snapshot of execution parameters; names omit '$'.</summary>
public sealed class QueryParameters : IReadOnlyDictionary<string, MixedValue>
{
  private readonly Dictionary<string, MixedValue> values;
  internal IRow? CorrelatedRow { get; }
  private QueryParameters(Dictionary<string, MixedValue> values, IRow row)
  {
    this.values = values;
    CorrelatedRow = row;
  }
  internal QueryParameters Correlate(IRow row) => new(values, row);
  public static QueryParameters Empty { get; } = new(Array.Empty<KeyValuePair<string, MixedValue>>());
  public QueryParameters(IEnumerable<KeyValuePair<string, MixedValue>> values)
  {
    ArgumentNullException.ThrowIfNull(values);
    this.values = values.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
  }
  public MixedValue this[string key] => values[key];
  public IEnumerable<string> Keys => values.Keys;
  public IEnumerable<MixedValue> Values => values.Values;
  public int Count => values.Count;
  public bool ContainsKey(string key) => values.ContainsKey(key);
  public bool TryGetValue(string key, out MixedValue value) => values.TryGetValue(key, out value);
  public IEnumerator<KeyValuePair<string, MixedValue>> GetEnumerator() => values.GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  internal MixedValue Require(ParameterNode parameter)
  {
    if (values.TryGetValue(parameter.Name, out var value)) return value;
    var error = new RuntimeException($"Missing parameter '${parameter.Name}'.", CypherErrorCode.MissingParameter);
    error.Locate(parameter.SourceSpan);
    throw error;
  }
}
