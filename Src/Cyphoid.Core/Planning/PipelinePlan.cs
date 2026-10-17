using Cyphoid.Core.Execution;

namespace Cyphoid.Core.Planning
{
  public abstract record PipelinePlan : LogicalPlan
  {
    internal IReadOnlyList<SyntaxTree.ParameterNode> ParameterReferences { get; init; } = [];
    public IOperator BuildExecutionPlan(IOperatorFactory factory) => new ParameterExecutionOperator(Compile(factory), ParameterReferences);
    public abstract Func<QueryParameters, IOperator> Compile(IOperatorFactory factory);
  }
}
