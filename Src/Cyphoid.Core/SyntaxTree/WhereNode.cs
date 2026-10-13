using System.Text;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record WhereNode(ExprNode Expr) : AstNode
  {
    public PipelinePlan BuildQueryPlan(PipelinePlan input)
    {
      throw new NotImplementedException();
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      throw new NotImplementedException();
    }
  }
}
