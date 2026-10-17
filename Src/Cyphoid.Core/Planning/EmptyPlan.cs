using System.Text;
using Cyphoid.Core.Execution;

namespace Cyphoid.Core.Planning
{
  public record EmptyPlan() : PipelinePlan
  {
    public override Func<QueryParameters, IOperator> Compile(IOperatorFactory factory)
    {
      return _ => factory.BuildEmptyResult();
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      sb.AppendLine("Empty dataset");
    }
  }
}
