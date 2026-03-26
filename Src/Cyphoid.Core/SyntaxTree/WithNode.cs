using Cyphoid.Core.Execution;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record WithNode(
    ProjectionsNode projections,
    IRowColumn[] OutputColumns) : ClauseNode(OutputColumns)
  {
    public override PipelinePlan<TId> BuildQueryPlan<TId>(
      PipelinePlan<TId>? input, 
      IRowColumn[] columns) 
    {
      throw new NotImplementedException();
    }
  }
}
