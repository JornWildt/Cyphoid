using System.Text;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record WhereNode(ExprNode Expr) : AstNode
  {
    public PipelinePlan<TId> BuildQueryPlan<TId>(PipelinePlan<TId> input) where TId : IEquatable<TId>
    {
      return new FilterPlan<TId>(input, Expr);
    }
    
    
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("WHERE ");
      Expr.PrettyPrint(sb);
    }
  }
}
