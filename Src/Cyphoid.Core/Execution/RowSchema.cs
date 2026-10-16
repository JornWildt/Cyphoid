using System.Collections;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.Execution;

/// <summary>An immutable snapshot of positional column definitions, shared by rows.</summary>
public sealed class RowSchema : IReadOnlyList<IRowColumn>
{
  private readonly IRowColumn[] columns;
  public static RowSchema Empty { get; } = new(Array.Empty<IRowColumn>());

  public RowSchema(IEnumerable<IRowColumn> columns)
  {
    ArgumentNullException.ThrowIfNull(columns);
    this.columns = columns.Select((column, index) =>
    {
      ArgumentNullException.ThrowIfNull(column);
      if (column.Index != index || column.Name == null || !Enum.IsDefined(column.Type))
        throw new ArgumentException("Schema columns must have sequential indexes, non-null names, and valid types.", nameof(columns));
      // Interfaces can be implemented by mutable host classes. Snapshot fields too.
      return (IRowColumn)new Column(column.Index, column.Name, column.Type);
    }).ToArray();
    if (this.columns.Select(c => c.Name).Distinct(StringComparer.Ordinal).Count() != this.columns.Length)
      throw new ArgumentException("Schema column names must be unique.", nameof(columns));
  }

  public IReadOnlyList<IRowColumn> Columns => this;
  public int Count => columns.Length;
  public IRowColumn this[int index] => columns[index];
  public IEnumerator<IRowColumn> GetEnumerator() => ((IEnumerable<IRowColumn>)columns).GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  private sealed record Column(int Index, string Name, MixedValue.ValueType Type) : IRowColumn;
}
