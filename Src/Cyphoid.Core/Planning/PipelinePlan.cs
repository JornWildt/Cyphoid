using Cyphoid.Core.Execution;

namespace Cyphoid.Core.Planning
{
  public abstract record PipelinePlan : LogicalPlan
  {
    public abstract IOperator BuildExecutionPlan(IOperatorFactory factory);
  }
}
