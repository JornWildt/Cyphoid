# Backend Implementation Guide

This document explains how to implement a backend for Cyphoid by providing an `IOperatorFactory<TId>` and a small set of data-access operators. The easiest path is to inherit from `ReferenceOperatorFactory<TId>`, which supplies all generic operators (filter, projection, aggregation, order by, etc.) using `IAsyncEnumerable` row pipelines. You then implement the few abstract methods that perform real graph lookups.

## Key concepts
- **Rows are positional**: each variable has a fixed `SlotIndex` (see `VariableDefinition`), and operators write values into `IRow<TId>.Values[slot]`.
- **Values are `MixedValue`**: nodes and properties are transported through rows as `MixedValue` instances.
- **Operators are async streams**: every `IOperator<TId>` exposes `IAsyncEnumerable<IRow<TId>> ExecuteAsync(IQueryContext context)`.

## Recommended approach
Create a concrete operator factory by inheriting from `ReferenceOperatorFactory<TId>` and implementing the abstract methods. These are the only required backend hooks.

Required overrides:
- `NewRow(IRowColumn[] matchColumns)` - create your row implementation.
- `BuildNodeScan(...)` - fetch initial nodes for `MATCH` patterns.
- `BuildExpandAll(...)` - expand relationships from a bound source node to a new (unbound) destination.

You can see a working example in `Src/Cyphoid.Tests/TestBackend/TestOperatorFactory.cs` and the test operators under `Src/Cyphoid.Tests/TestBackend/Operators/`.

## Minimal skeleton
```csharp
internal sealed class MyOperatorFactory : ReferenceOperatorFactory<string>
{
  protected override IRow<string> NewRow(IRowColumn[] matchColumns)
    => new Row<string>(matchColumns);

  protected override IOperator<string> BuildNodeScan(
    VariableDefinition variable,
    string? label,
    PropertyFilter? propertyFilter,
    IRowColumn[] matchColumns)
    => new MyNodeScanOperator(...);

  protected override IOperator<string> BuildExpandAll(
    IOperator<string> input,
    VariableDefinition sourceVariable,
    ExpandDirectionType direction,
    string? relationLabel,
    VariableDefinition destinationVariable,
    string? destinationLabel,
    PropertyFilter? destinationPropertyFilter)
    => new MyExpandAllOperator(...);
}
```

## Operator responsibilities

### NewRow
- Return a row that matches the provided `matchColumns`.
- The row must be able to hold values for every declared variable slot.

### Node scan (BuildNodeScan)
- Produce one row per matching node in your storage layer.
- Apply `label` and `propertyFilter` when provided.
- For each match, set `row.Values[variable.SlotIndex]` to a `MixedValue` that wraps your graph node.
- Use `matchColumns` when constructing the row so downstream operators can access the correct metadata.

### Expand all (BuildExpandAll)
- Read the bound source node from `sourceVariable.SlotIndex` in each input row.
- Traverse relationships based on:
  - `direction` (incoming/outgoing)
  - `relationLabel` (relationship type)
  - `destinationLabel` (node label)
  - `destinationPropertyFilter` (node properties)
- For each match, emit a new row where `destinationVariable.SlotIndex` is filled with the destination node value.
- Preserve existing row values (copy/clone if your row type is mutable).

## Optional overrides (performance)
`ReferenceOperatorFactory<TId>` implements these generically, but you can override them for better performance or pushdowns:
- `BuildExpandInto` (relationship check between already-bound nodes)
- `BuildFilter` (WHERE)
- `BuildProjection` and `BuildAggregationProjection`
- `BuildOrderBy`, `BuildLimit`, `BuildCartesianProduct`

If you override, preserve semantics and output row shapes exactly.

## Async behavior and correctness
- `ExecuteAsync` should be a true async stream. If you have no async work, a single `await Task.Yield()` keeps the method async.
- Do not block on I/O; use async DB drivers where possible.
- Ensure your operators are deterministic for the same input rows.

## Example implementation
The test backend shows a complete minimal implementation:
- Factory: `Src/Cyphoid.Tests/TestBackend/TestOperatorFactory.cs`
- Node scan: `Src/Cyphoid.Tests/TestBackend/Operators/NodeScanTestOperator.cs`
- Graph store: `Src/Cyphoid.Tests/TestBackend/InMemoryGraph.cs`
