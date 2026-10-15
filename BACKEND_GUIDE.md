# Backend Implementation Guide

This document explains how to implement a backend for Cyphoid by providing an `IOperatorFactory` and a small set of data-access operators. The easiest path is to inherit from `ReferenceOperatorFactory`, which supplies all generic operators (filter, projection, aggregation, order by, etc.) using `IAsyncEnumerable` row pipelines. You then implement the few abstract methods that perform real graph lookups.

## Key concepts
- **Rows are positional**: each variable has a fixed `SlotIndex` (see `VariableDefinition`), and operators write values into `IRow.Values[slot]`.
- **Values are `MixedValue`**: nodes and properties are transported through rows as `MixedValue` instances.
- **Operators are async streams**: every `IOperator` exposes `IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context)`.

## Recommended approach
As of 0.13.0, `BuildExpandAll` and `BuildExpandInto` have no `optional` parameter. Expansion operators must emit only matches. The enclosing clause-level optional-match operator preserves an incoming row with null new bindings when the whole pattern and WHERE have no matches; backend expansion operators must not supply their own fallback rows.

Graph nodes implement the non-generic `IGraphNode` interface, including `MixedValue GetId()`. The scalar function `id(node)` returns this value and `id(null)` returns null. IDs retain their backend-defined query type, such as string or integer. Relationship IDs are not currently supported.

`GraphNode<TId>.GetId()` uses `MixedValue.FromObject(Id)`. If your ID type is not supported by that conversion (for example, `Guid`), override `GetId()` to choose a supported query representation, such as `MixedValue.String(Id.ToString())`.

Create a concrete operator factory by inheriting from `ReferenceOperatorFactory` and implementing the abstract methods. These are the only required backend hooks.

Required overrides:
- `BuildNodeScan(...)` - fetch initial nodes for `MATCH` patterns.
- `BuildExpandAll(...)` - expand relationships from a bound source node to a new (unbound) destination.

You can see a working example in `Src/Cyphoid.Tests/TestBackend/TestOperatorFactory.cs` and the test operators under `Src/Cyphoid.Tests/TestBackend/Operators/`.

## Minimal skeleton
```csharp
internal sealed class MyOperatorFactory : ReferenceOperatorFactory
{
  protected override IOperator BuildNodeScan(
    VariableDefinition variable,
    string? label,
    PropertyFilter? propertyFilter,
    string? targetRelationshipLabel,
    IRowColumn[] matchColumns)
    => new MyNodeScanOperator(...);

  protected override IOperator BuildExpandAll(
    IOperator input,
    VariableDefinition sourceVariable,
    ExpandDirectionType direction,
    string? relationLabel,
    VariableDefinition destinationVariable,
    string? destinationLabel,
    PropertyFilter? destinationPropertyFilter,
    IRowColumn[] matchColumns)
    => new MyExpandAllOperator(...);
}
```

## Operator responsibilities

### Row creation
- Supply an `IQueryRowFactory` through `IQueryContext.RowFactory`; row creation is separate from the operator factory.
- Implement both `EmptyRow(IReadOnlyList<IRowColumn> columns)` and `EmptyRow(IRow source, IReadOnlyList<IRowColumn> columns)`. The second overload can preserve backend metadata from a source row.
- Return an `IRow` with the supplied columns and enough values for every declared variable slot. The reference `Row` class is available if no custom row implementation is needed.

### Node scan (BuildNodeScan)
- Produce one row per matching node in your storage layer.
- Apply `label` and `propertyFilter` when provided.
- `targetRelationshipLabel` is a hint for the following relationship expansion; any optimization using it must preserve the scan's matching semantics.
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
`ReferenceOperatorFactory` implements these generically, but you can override them for better performance or pushdowns:
- `BuildExpandInto` (relationship check between already-bound nodes)
- `BuildFilter` (WHERE)
- `BuildProjection`, `BuildDistinct`, and `BuildAggregateProjection` (the last implements `IOperatorFactory.BuildAggregationProjection`)
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
