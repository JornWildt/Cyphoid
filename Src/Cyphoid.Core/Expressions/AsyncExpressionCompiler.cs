using Cyphoid.Core.Execution;
using Cyphoid.Core.Exceptions;
using Cyphoid.Core.Expressions.Functions;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Expressions;

internal delegate ValueTask<MixedValue> AsyncRowEvaluator(IRow row, IQueryContext context, CancellationToken ct);

internal static class AsyncExpressionCompiler
{
  internal static bool ContainsExists(this ExprNode expression) => expression is ExistsSubqueryNode || Children(expression).Any(ContainsExists);

  internal static AsyncRowEvaluator CompileAsync(this ExprNode expression, IOperatorFactory factory, QueryParameters parameters)
  {
    var evaluate = Build(expression, factory, parameters);
    return async (row, context, ct) =>
    {
      ct.ThrowIfCancellationRequested();
      try { return await evaluate(row, context, ct); }
      catch (RuntimeException error) { error.Locate(expression.SourceSpan); throw; }
    };
  }

  private static AsyncRowEvaluator Build(ExprNode expression, IOperatorFactory factory, QueryParameters parameters)
  {
    if (!expression.ContainsExists())
    {
      var sync = expression.Bind(parameters).CompileEvaluator();
      return (row, _, _) => ValueTask.FromResult(sync(row));
    }
    if (expression is ExistsSubqueryNode exists)
    {
      var branches = exists.Branches.Select(b => b.Compile(factory)).ToArray();
      return async (row, context, ct) =>
      {
        var correlated = parameters.Correlate(row);
        foreach (var branch in branches)
        {
          ct.ThrowIfCancellationRequested();
          await using var results = branch(correlated).ExecuteAsync(context, ct).GetAsyncEnumerator(ct);
          var found = await results.MoveNextAsync();
          ct.ThrowIfCancellationRequested();
          if (found) return MixedValue.Bool(true);
        }
        return MixedValue.Bool(false);
      };
    }

    var children = Children(expression).Select(e => e.CompileAsync(factory, parameters)).ToArray();
    if (expression is BinaryOperatorNode { Operator: BinaryOperatorType.And or BinaryOperatorType.Or } binary)
      return async (row, context, ct) =>
      {
        var left = (await children[0](row, context, ct)).AsBool();
        if (binary.Operator == BinaryOperatorType.And && !left) return MixedValue.Bool(false);
        if (binary.Operator == BinaryOperatorType.Or && left) return MixedValue.Bool(true);
        return MixedValue.Bool((await children[1](row, context, ct)).AsBool());
      };
    if (expression is FunctionCallNode { Definition: CoalesceFunction })
      return async (row, context, ct) =>
      {
        foreach (var child in children)
        {
          var value = await child(row, context, ct);
          if (!value.IsNull()) return value;
        }
        return MixedValue.Null();
      };
    if (expression is InOperatorNode)
      return async (row, context, ct) =>
      {
        var value = await children[0](row, context, ct);
        var unknown = false;
        for (var i = 1; i < children.Length; i++)
        {
          var equal = MixedValue.QueryEquals(value, await children[i](row, context, ct));
          if (equal.IsNull()) unknown = true;
          else if (equal.AsBool()) return MixedValue.Bool(true);
        }
        return unknown ? MixedValue.Null() : MixedValue.Bool(false);
      };

    // Reuse the existing scalar semantics. Direct children become positional
    // values in a temporary evaluation row, never a host/backend result row.
    var schema = new RowSchema(Enumerable.Range(0, children.Length).Select(i => new RowColumn(i, "arg" + i, MixedValue.ValueType.Null)));
    var slots = Enumerable.Range(0, children.Length).Select(i => (ExprNode)new VariableExprNode(
      new VariableDefinition(true, "arg" + i, MixedValue.ValueType.Null, i))).ToArray();
    var scalar = ReplaceChildren(expression, slots).CompileEvaluator();
    return async (row, context, ct) =>
    {
      IRow values = new Row(schema);
      for (var i = 0; i < children.Length; i++)
      {
        values.Values[i] = await children[i](row, context, ct);
        if (i == 0 && expression is MapProjectionNode)
        {
          var target = values.Values[0]!.Value;
          if (target.IsNull()) return MixedValue.Null();
          if (target.Type is not (MixedValue.ValueType.Map or MixedValue.ValueType.Node)) return scalar(values);
        }
      }
      return scalar(values);
    };
  }

  private static IEnumerable<ExprNode> Children(ExprNode expression) => expression switch
  {
    BinaryOperatorNode n => [n.Left, n.Right], UnaryOperatorNode n => [n.Expr],
    NullCheckNode n => [n.Expr], SubExpression n => [n.Sub],
    InOperatorNode n => new[] { n.Expr }.Concat(n.Items), FunctionCallNode n => n.Parameters,
    MapLiteralNode n => n.Entries.Select(e => e.Value), MapAccessNode n => [n.Target, n.Key],
    MapProjectionNode n => new[] { n.Target }.Concat(n.Entries.Where(e => e.Value != null).Select(e => e.Value!)),
    _ => []
  };

  private static ExprNode ReplaceChildren(ExprNode expression, ExprNode[] children)
  {
    var i = 1;
    return expression switch
    {
      BinaryOperatorNode n => n with { Left = children[0], Right = children[1] },
      UnaryOperatorNode n => n with { Expr = children[0] }, NullCheckNode n => n with { Expr = children[0] },
      SubExpression n => n with { Sub = children[0] }, FunctionCallNode n => n with { Parameters = children },
      MapLiteralNode n => n with { Entries = n.Entries.Select((e, index) => e with { Value = children[index] }).ToArray() },
      MapAccessNode n => n with { Target = children[0], Key = children[1] },
      MapProjectionNode n => n with { Target = children[0], Entries = n.Entries.Select(e => e.Value == null ? e : e with { Value = children[i++] }).ToArray() },
      _ => throw new InvalidOperationException($"Unsupported asynchronous expression {expression.GetType().Name}.")
    };
  }
}
