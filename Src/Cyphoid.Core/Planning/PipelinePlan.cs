using Cyphoid.Core.Execution;

namespace Cyphoid.Core.Planning
{
  public abstract record PipelinePlan<TId> : LogicalPlan<TId> where TId : IEquatable<TId>
  {
    public abstract IOperator<TId> BuildExecutionPlan(IOperatorFactory<TId> factory);
  }
}
