using Cyphoid.Core.Execution;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public abstract record ClauseNode() : AstNode
  {
    public abstract PipelinePlan BuildQueryPlan(
      PipelinePlan? input);
  }
}
