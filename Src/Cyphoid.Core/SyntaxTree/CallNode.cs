using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Planning;
using Cyphoid.Core.Procedures;

namespace Cyphoid.Core.SyntaxTree;

public sealed record ProcedureYield(int OutputIndex, string FieldName, VariableDefinition Variable);

public sealed record CallNode(
  ProcedureRegistration Registration,
  IReadOnlyList<ExprNode> Arguments,
  IReadOnlyList<ProcedureYield> Yields,
  RowSchema OutputColumns,
  ExprNode? Predicate) : ClauseNode
{
  public override PipelinePlan BuildQueryPlan(PipelinePlan? input)
  {
    PipelinePlan plan = new ProcedureCallPlan(input ?? new EmptyPlan(), this);
    return Predicate == null ? plan : new FilterPlan(plan, Predicate);
  }

  public override void PrettyPrint(StringBuilder sb)
  {
    sb.Append("CALL ").Append(Registration.QualifiedName).Append('(');
    for (var i = 0; i < Arguments.Count; i++)
    {
      if (i > 0) sb.Append(", ");
      Arguments[i].PrettyPrint(sb);
    }
    sb.Append(") YIELD ");
    for (var i = 0; i < Yields.Count; i++)
    {
      if (i > 0) sb.Append(", ");
      var binding = Yields[i];
      sb.Append(binding.FieldName);
      if (binding.FieldName != binding.Variable.Name)
        sb.Append(" AS ").Append(binding.Variable.Name);
    }
    if (Predicate != null)
    {
      sb.Append(" WHERE ");
      Predicate.PrettyPrint(sb);
    }
  }
}
