using System.Text;
using Cyphoid.Core.Execution;

namespace Cyphoid.Core.Planning
{
  public record EmptyPlan<TId>() : PipelinePlan<TId> where TId : IEquatable<TId>
  {
    public override IOperator<TId> BuildExecutionPlan(IOperatorFactory<TId> factory)
    {
      return factory.BuildEmptyResult();
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      sb.AppendLine("Empty dataset");
    }
  }
}
