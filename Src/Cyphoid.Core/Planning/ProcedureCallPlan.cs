using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning;

public sealed record ProcedureCallPlan(PipelinePlan Input, CallNode Call) : PipelinePlan
{
  public override Func<QueryParameters, IOperator> Compile(IOperatorFactory factory)
  {
    var input = Input.Compile(factory);
    var schema = new RowSchema(Call.Registration.Outputs.Select((f, i) => new RowColumn(i, f.Name, f.Type)));
    return parameters => new ProcedureCallReferenceOperator(input(parameters), Call,
      Call.Arguments.Select(a => a.Bind(parameters).CompileEvaluator()).ToArray(), schema);
  }

  public override void PrettyPrint(StringBuilder sb)
  {
    Call.PrettyPrint(sb);
    sb.AppendLine();
    Input.PrettyPrint(sb);
  }
}
