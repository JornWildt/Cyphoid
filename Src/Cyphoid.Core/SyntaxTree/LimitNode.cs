using System.Text;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record LimitNode(long Limit) : AstNode
  {
    public PipelinePlan BuildQueryPlan(PipelinePlan input)
    {
      return new LimitPlan(input, Limit);
    }
    
    
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("LIMIT " + Limit);
    }
  }
}
