using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Core.Planning
{
  public record UnionPlan(IReadOnlyList<PipelinePlan> Branches, bool All) : PipelinePlan
  {
    public override IOperator BuildExecutionPlan(IOperatorFactory factory)
      => new UnionReferenceOperator(Branches.Select(b => b.BuildExecutionPlan(factory)).ToArray(), All);

    public override void PrettyPrint(StringBuilder sb)
    {
      sb.AppendLine(All ? "UNION ALL" : "UNION");
      foreach (var branch in Branches)
        branch.PrettyPrint(sb);
    }
  }
}
