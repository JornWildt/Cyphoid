using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record QueryNode(
    IReadOnlyList<ClauseNode> Clauses,
    ReturnLimitNode ReturnLimit,
    IReadOnlyList<QueryNode>? UnionBranches = null,
    bool UnionAll = false) : AstNode
  {
    public IReadOnlyList<ParameterNode> ParameterReferences { get; init; } = [];
    public IReadOnlyList<string> RequiredParameters => Array.AsReadOnly(ParameterReferences.Select(p => p.Name).ToArray());
    public PipelinePlan BuildQueryPlan()
    {
      PipelinePlan? plan = null;
      foreach (var c in Clauses)
      {
        plan = c.BuildQueryPlan(plan);
      }
      
      plan = plan ?? new EmptyPlan();

      var projectionPlan = ReturnLimit.BuildProjectionPlan(plan);
      if (UnionBranches is { Count: > 0 })
        return new UnionPlan(new[] { projectionPlan }.Concat(UnionBranches.Select(b => b.BuildQueryPlan())).ToArray(), UnionAll)
          { ParameterReferences = ParameterReferences };
      return projectionPlan with { ParameterReferences = ParameterReferences };
    }


    public override void PrettyPrint(StringBuilder sb)
    {
      var first = true;
      foreach (var c in Clauses)
      {
        if (!first)
          sb.Append(" ");
        c.PrettyPrint(sb);
        first = false;
      }

      if (!first)
        sb.Append(" ");

      ReturnLimit.PrettyPrint(sb);
      if (UnionBranches != null)
        foreach (var branch in UnionBranches)
        {
          sb.Append(UnionAll ? " UNION ALL " : " UNION ");
          branch.PrettyPrint(sb);
        }
    }
  }
}
