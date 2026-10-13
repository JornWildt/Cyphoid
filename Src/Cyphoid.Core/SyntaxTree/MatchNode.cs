using System.Text;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record MatchNode(bool Optional, PatternNode Pattern) : AstNode
  {
    public PipelinePlan BuildQueryPlan()
    {
      throw new NotImplementedException();
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      throw new NotImplementedException();
    }
  }
}
