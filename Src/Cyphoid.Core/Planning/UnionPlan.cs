using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Core.Planning
{
  public record UnionPlan(IReadOnlyList<PipelinePlan> Branches, bool All) : PipelinePlan
  {
    public override Func<QueryParameters, IOperator> Compile(IOperatorFactory factory)
    {
      var branches = Branches.Select(b => b.Compile(factory)).ToArray();
      return parameters => new UnionReferenceOperator(branches.Select(b => b(parameters)).ToArray(), All);
    }

    public override void PrettyPrint(StringBuilder sb)
    {
      sb.AppendLine(All ? "UNION ALL" : "UNION");
      foreach (var branch in Branches)
        branch.PrettyPrint(sb);
    }
  }
}
