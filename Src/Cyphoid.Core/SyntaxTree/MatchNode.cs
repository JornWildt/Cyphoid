using System.Text;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record MatchNode(PatternNode Pattern) : AstNode
  {
    public PipelinePlan<TId> BuildQueryPlan<TId>() where TId : IEquatable<TId>
    {
      throw new NotImplementedException();
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      throw new NotImplementedException();
    }
  }
}
