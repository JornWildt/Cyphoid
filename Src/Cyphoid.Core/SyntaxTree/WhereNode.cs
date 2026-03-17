using System.Text;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record WhereNode(ExprNode Expr) : AstNode
  {
    public LogicalPlan BuildQueryPlan(LogicalPlan input)
    {
      return new FilterPlan(input, Expr);
    }
    
    
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("WHERE ");
      Expr.PrettyPrint(sb);
    }
  }
}
