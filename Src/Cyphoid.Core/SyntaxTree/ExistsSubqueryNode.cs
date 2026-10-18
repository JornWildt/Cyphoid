using System.Runtime.CompilerServices;
using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Planning;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.SyntaxTree;

public sealed record ExistsBranchNode(IReadOnlyList<ClauseNode> Clauses, ReturnLimitNode? Return) : AstNode
{
  internal Func<QueryParameters, IOperator> Compile(IOperatorFactory factory)
  {
    PipelinePlan plan = new CorrelatedInputPlan();
    foreach (var match in Clauses)
    {
      plan = match.BuildQueryPlan(plan);
    }
    if (Return != null) plan = Return.BuildProjectionPlan(plan);
    return plan.Compile(factory);
  }
  public override void PrettyPrint(StringBuilder sb)
  {
    for (var i = 0; i < Clauses.Count; i++)
    {
      if (i > 0) sb.Append(' ');
      Clauses[i].PrettyPrint(sb);
    }
    if (Return != null)
    {
      if (Clauses.Count > 0) sb.Append(' ');
      Return.PrettyPrint(sb);
    }
  }

  private sealed record CorrelatedInputPlan : PipelinePlan
  {
    public override Func<QueryParameters, IOperator> Compile(IOperatorFactory factory) => parameters => new Input(
      parameters.CorrelatedRow ?? throw new InvalidOperationException("Missing correlated row."));
    public override void PrettyPrint(StringBuilder sb) => sb.Append("Correlated input");
  }
  private sealed record Input(IRow Row) : IOperator
  {
    public async IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context, [EnumeratorCancellation] CancellationToken ct = default)
    {
      ct.ThrowIfCancellationRequested();
      await Task.CompletedTask;
      yield return Row;
    }
  }
}

internal sealed record ImportedValueNode(VariableDefinition Variable) : ExprNode(ValueKindType.Variable)
{
  public override RowEvaluator BuildEvaluator() => throw new InvalidOperationException("Outer values must be bound before evaluation.");
  public override void PrettyPrint(StringBuilder sb) => sb.Append(Variable.Name);
}

public sealed record ExistsSubqueryNode(IReadOnlyList<ExistsBranchNode> Branches, bool UnionAll) : ExprNode(ValueKindType.Variable)
{
  public override RowEvaluator BuildEvaluator() => throw new InvalidOperationException("EXISTS requires asynchronous evaluation.");
  public override void PrettyPrint(StringBuilder sb)
  {
    sb.Append("EXISTS { ");
    for (var i = 0; i < Branches.Count; i++)
    {
      if (i > 0) sb.Append(UnionAll ? " UNION ALL " : " UNION ");
      Branches[i].PrettyPrint(sb);
    }
    sb.Append(" }");
  }
}
