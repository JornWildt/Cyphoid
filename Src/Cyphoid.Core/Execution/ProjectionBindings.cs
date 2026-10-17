using System.Collections;

namespace Cyphoid.Core.Execution;

/// <summary>Bound projection evaluators retaining their compiled template's output schema.</summary>
public sealed class ProjectionBindings(IReadOnlyList<ProjectionEvaluator> values, RowSchema schema)
  : IReadOnlyList<ProjectionEvaluator>
{
  public RowSchema Schema { get; } = schema;
  public int Count => values.Count;
  public ProjectionEvaluator this[int index] => values[index];
  public IEnumerator<ProjectionEvaluator> GetEnumerator() => values.GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
