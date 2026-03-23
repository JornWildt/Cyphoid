using System.Text;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record ReturnNode(IReadOnlyList<ReturnProjectionNode> Projections) : AstNode
  {
    //public ProjectionPlan<TId> BuildQueryPlan<TId>(PipelinePlan<TId> input) where TId : IEquatable<TId>
    //{
    //  return new ProjectionPlan<TId>(input, Items);
    //}


    //public override void PrettyPrint(StringBuilder sb)
    //{
    //  sb.Append("RETURN ");
    //  bool first = true;
    //  foreach (var i in Items)
    //  {
    //    if (!first)
    //      sb.Append(", ");
    //    i.PrettyPrint(sb);
    //    first = false;
    //  }
    //}
  }


  public record ReturnProjectionNode(ExprNode Expr, bool IsAnonymous, string Identifier) : AstNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      Expr.PrettyPrint(sb);
      if (Identifier != null && !IsAnonymous)
      {
        sb.Append(" AS ");
        sb.Append(Identifier);
      }
    }
  }
}
