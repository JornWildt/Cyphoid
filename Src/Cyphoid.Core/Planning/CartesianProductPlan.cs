using System.Text;
using Cyphoid.Core.Execution;

namespace Cyphoid.Core.Planning
{
  public record CartesianProductPlan(
    PipelinePlan Left,
    PipelinePlan Right) : PipelinePlan
  {
    public override Func<QueryParameters, IOperator> Compile(IOperatorFactory factory)
    {
      var left = Left.Compile(factory);
      var right = Right.Compile(factory);
      return parameters => factory.BuildCartesianProduct(left(parameters), right(parameters));
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
