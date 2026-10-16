using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning;

public sealed record ProcedureCallPlan(PipelinePlan Input, CallNode Call) : PipelinePlan
{
  public override IOperator BuildExecutionPlan(IOperatorFactory factory) =>
    new ProcedureCallReferenceOperator(Input.BuildExecutionPlan(factory), Call,
      Call.Arguments.Select(a => a.CompileEvaluator()).ToArray());

  public override void PrettyPrint(StringBuilder sb)
  {
    Call.PrettyPrint(sb);
    sb.AppendLine();
    Input.PrettyPrint(sb);
  }
}
