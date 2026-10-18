using System.Runtime.CompilerServices;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.ReferenceBackend;

internal sealed record AsyncFilterOperator(IOperator Input, AsyncRowEvaluator Predicate) : IOperator
{
  public async IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context, [EnumeratorCancellation] CancellationToken ct = default)
  {
    await foreach (var row in Input.ExecuteAsync(context, ct).WithCancellation(ct))
      if (!(await Predicate(row, context, ct)).IsAnythingButTrue()) yield return row;
  }
}

internal sealed class AsyncMaterializeOperator(IOperator input, IReadOnlyList<AsyncRowEvaluator> evaluators) : IOperator
{
  internal RowSchema? InputSchema { get; private set; }
  public async IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context, [EnumeratorCancellation] CancellationToken ct = default)
  {
    RowSchema? schema = null;
    await foreach (var row in input.ExecuteAsync(context, ct).WithCancellation(ct))
    {
      ct.ThrowIfCancellationRequested();
      InputSchema ??= row.Schema;
      if (schema == null)
      {
        var names = row.Columns.Select(c => c.Name).ToHashSet(StringComparer.Ordinal);
        var columns = row.Columns.ToList();
        for (var i = 0; i < evaluators.Count; i++)
        {
          var name = "_exists" + i;
          while (!names.Add(name)) name += "_";
          columns.Add(new RowColumn(columns.Count, name, MixedValue.ValueType.Null));
        }
        schema = new RowSchema(columns);
      }
      var result = row.CopyAndExpand(schema);
      for (var i = 0; i < evaluators.Count; i++)
        result.Values[row.Values.Length + i] = await evaluators[i](row, context, ct);
      yield return result;
    }
  }
}

internal sealed record AsyncStageOperator(IOperator Input, IReadOnlyList<AsyncRowEvaluator> Evaluators,
  Func<IOperator, IOperator> Build, bool Trim) : IOperator
{
  public async IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context, [EnumeratorCancellation] CancellationToken ct = default)
  {
    var materialized = new AsyncMaterializeOperator(Input, Evaluators);
    await foreach (var row in Build(materialized).ExecuteAsync(context, ct).WithCancellation(ct))
    {
      ct.ThrowIfCancellationRequested();
      if (!Trim) { yield return row; continue; }
      var result = context.RowFactory.EmptyRow(row, materialized.InputSchema!);
      Array.Copy(row.Values, result.Values, result.Values.Length);
      yield return result;
    }
  }
}

internal sealed record TailValueNode(int Index, int Count) : ExprNode(ValueKindType.Variable)
{
  public override RowEvaluator BuildEvaluator() => row => row.Values[row.Values.Length - Count + Index] ?? MixedValue.Null();
}

internal sealed class AsyncMaterialization(IOperatorFactory factory, QueryParameters parameters)
{
  internal List<AsyncRowEvaluator> Evaluators { get; } = [];
  internal ExprNode Transform(ExprNode expression)
  {
    if (!expression.ContainsExists()) return expression.Bind(parameters);
    if (expression.ValueKind == ValueKindType.Aggregate)
      return expression switch
      {
        FunctionCallNode n => n with { Parameters = n.Parameters.Select(Transform).ToArray() },
        SubExpression n => n with { Sub = Transform(n.Sub) },
        _ => throw new InvalidOperationException("Unsupported aggregate shape.")
      };
    var index = Evaluators.Count;
    Evaluators.Add(expression.CompileAsync(factory, parameters));
    return new DeferredTailNode(index, this) { SourceSpan = expression.SourceSpan };
  }
  private sealed record DeferredTailNode(int Index, AsyncMaterialization Owner) : ExprNode(ValueKindType.Variable)
  {
    public override RowEvaluator BuildEvaluator() => new TailValueNode(Index, Owner.Evaluators.Count).BuildEvaluator();
  }
}
