using System.Text;
using Cyphoid.Core.Execution;

namespace Cyphoid.Core.Planning
{
  public record EmptyPlan() : PipelinePlan
  {
    public override IOperator BuildExecutionPlan(IOperatorFactory factory)
    {
      return factory.BuildEmptyResult();
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      sb.AppendLine("Empty dataset");
    }
  }
}
