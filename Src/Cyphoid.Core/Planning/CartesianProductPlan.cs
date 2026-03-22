using System.Text;
using Cyphoid.Core.Execution;

namespace Cyphoid.Core.Planning
{
  public record CartesianProductPlan<TId>(
    PipelinePlan<TId> Left,
    PipelinePlan<TId> Right) : PipelinePlan<TId> where TId : IEquatable<TId>
  {
    public override IOperator<TId> BuildExecutionPlan(IOperatorFactory<TId> factory)
    {
      return factory.BuildCartesianProduct(
        Left.BuildExecutionPlan(factory),
        Right.BuildExecutionPlan(factory));
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      sb.AppendLine("Cartesian product");
      // FIXME: Indents
      Left.PrettyPrint(sb);
      Right.PrettyPrint(sb);
    }
  }
}
