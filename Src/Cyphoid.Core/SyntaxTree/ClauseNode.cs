using Cyphoid.Core.Execution;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public abstract record ClauseNode(IRowColumn[] OutputColumns) : AstNode
  {
    public abstract PipelinePlan<TId> BuildQueryPlan<TId>(
      PipelinePlan<TId>? input,
      IRowColumn[] columns) where TId : IEquatable<TId>;
  }
}
