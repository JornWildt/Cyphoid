using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions.Functions;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Core.Exceptions;

namespace Cyphoid.Core.SyntaxTree
{
  public record FunctionCallNode(
    string FunctionName,
    IFunctionDefinition Definition,
    IReadOnlyList<ExprNode> Parameters) : ExprNode(Definition.ValueKind)
  {
    public override RowEvaluator BuildEvaluator()
    {
      var parameters = Parameters.Select(p => p.CompileEvaluator()).ToArray();
      return Definition.GetRowEvaluator(parameters);
    }


    public override IAggregationEvaluator GetAggregationEvaluator(
      RowEvaluator expression,
      int outputSlotIndex)
    {
      var parameters = Parameters.Select(p => p.CompileEvaluator()).ToArray();
      return new LocatedAggregator(Definition.GetAggregator(parameters, outputSlotIndex), FunctionName, SourceSpan);
    }

    private sealed class LocatedAggregator(IAggregationEvaluator inner, string name, SourceSpan? span) : IAggregationEvaluator
    {
      public void Initialize() => Run(inner.Initialize);
      public void Accumulate(IRow row) => Run(() => inner.Accumulate(row));
      public void WriteResult(IRow row) => Run(() => inner.WriteResult(row));

      private void Run(Action action)
      {
        try { action(); }
        catch (RuntimeException cause)
        {
          // A parameter evaluator already points to its own failing expression.
          if (cause.SourceSpan != null) throw;
          var error = new RuntimeException($"{name}(): {cause.Message}", cause, cause.Code);
          error.Locate(span);
          throw error;
        }
      }
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      if (Definition is CountAllFunction)
      {
        sb.Append(FunctionName);
        sb.Append("(*)");
      }
      else
      {
        sb.Append(FunctionName);
        sb.Append("(");
        bool first = true;
        foreach (var p in Parameters)
        {
          if (!first)
            sb.Append(", ");
          p.PrettyPrint(sb);
          first = false;
        }
        sb.Append(")");
      }
    }
  }
}
