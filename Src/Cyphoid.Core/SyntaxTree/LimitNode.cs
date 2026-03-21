using System.Text;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record LimitNode(int Limit) : AstNode
  {
    public PipelinePlan<TId> BuildQueryPlan<TId>(PipelinePlan<TId> input) where TId : IEquatable<TId>
    {
      throw new NotImplementedException();
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      throw new NotImplementedException();
    }
  }
}
