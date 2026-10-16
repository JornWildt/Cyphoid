using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Core.Exceptions;

namespace Cyphoid.Core.SyntaxTree
{
  public abstract record AstNode
  {
    public SourceSpan? SourceSpan { get; internal set; }
    public virtual string PrettyPrint()
    {
      StringBuilder sb = new StringBuilder();
      PrettyPrint(sb);
      return sb.ToString();
    }

    public virtual void PrettyPrint(StringBuilder sb) { sb.Append($"-not-implemented({GetType().Name})-"); }
  }


  public record VariableNode(string Name) : AstNode;

  public record LabelNode(string Label) : AstNode;


  public enum ValueKindType { Const, Variable, Aggregate }

  public abstract record ExprNode(
    ValueKindType ValueKind) : AstNode
  {
    public abstract RowEvaluator BuildEvaluator();

    // Compile once, attaching the narrowest expression location on known query failures.
    // Backend bugs and arbitrary host exceptions are deliberately not translated here.
    public RowEvaluator CompileEvaluator()
    {
      var evaluate = BuildEvaluator();
      return row =>
      {
        try { return evaluate(row); }
        catch (RuntimeException error)
        {
          error.Locate(SourceSpan);
          throw;
        }
      };
    }

    public virtual IAggregationEvaluator GetAggregationEvaluator(RowEvaluator expression, int outputSlotIndex)
    {
      throw new NotImplementedException();
    }
  }
}
