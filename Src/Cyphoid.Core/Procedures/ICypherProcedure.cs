using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.Procedures;

/// <summary>A host-installed collection of procedures in one namespace.</summary>
public interface ICypherProcedurePlugin
{
  string Namespace { get; }
  IEnumerable<ICypherProcedure> GetProcedures();
}

public sealed record ProcedureField(string Name, MixedValue.ValueType Type, bool Nullable = false);

/// <summary>Instances must support concurrent invocations; keep invocation state local.</summary>
public interface ICypherProcedure
{
  string Name { get; }
  IReadOnlyList<ProcedureField> Parameters { get; }
  IReadOnlyList<ProcedureField> Outputs { get; }

  /// <summary>
  /// Return fresh host rows with the supplied local output schema. Use
  /// context.RowFactory.EmptyRow(inputRow, outputColumns) to preserve metadata.
  /// Do not mutate the input row or retain/mutate result rows after yielding them.
  /// </summary>
  IAsyncEnumerable<IRow> ExecuteAsync(
    IQueryContext context,
    IRow inputRow,
    IReadOnlyList<MixedValue> arguments,
    IReadOnlyList<IRowColumn> outputColumns,
    CancellationToken ct);
}
