using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning
{
  public record FilterPlan(
    PipelinePlan Input,
    ExprNode Predicate) : PipelinePlan
  {
    public override Func<QueryParameters, IOperator> Compile(IOperatorFactory factory)
    {
      var input = Input.Compile(factory);
      return parameters => factory.BuildFilter(input(parameters), Predicate.Bind(parameters).CompileEvaluator());
    }

    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("Filter: ");
      Predicate.PrettyPrint(sb);
      sb.AppendLine();
      Input.PrettyPrint(sb);
    }
  }
}
