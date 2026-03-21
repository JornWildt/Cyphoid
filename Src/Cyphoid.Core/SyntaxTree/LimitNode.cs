using System.Text;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record LimitNode(int Limit) : AstNode
  {
    public PipelinePlan<TId> BuildQueryPlan<TId>(PipelinePlan<TId> input) where TId : IEquatable<TId>
    {
      return new LimitPlan<TId>(input, Limit);
    }
    
    
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("LIMIT " + Limit);
    }
  }
}
