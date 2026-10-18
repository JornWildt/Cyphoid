using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;
using Cyphoid.Core.Expressions;
using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Core.Planning
{
  public record FilterPlan(
    PipelinePlan Input,
    ExprNode Predicate) : PipelinePlan
  {
    public override Func<QueryParameters, IOperator> Compile(IOperatorFactory factory)
    {
      var input = Input.Compile(factory);
      return parameters => Predicate.ContainsExists()
        ? new AsyncFilterOperator(input(parameters), Predicate.CompileAsync(factory, parameters))
        : factory.BuildFilter(input(parameters), Predicate.Bind(parameters).CompileEvaluator());
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
