using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning
{
  public record ExpandPlan(
    LogicalPlan Input,
    VariableDefinition Source,
    VariableDefinition Destination,
    RelationshipPatternNode Relationship) : LogicalPlan
  {
    public override IOperator BuildExecutionPlan(IOperatorFactory factory)
    {
      throw new NotImplementedException();
    }

    public override void PrettyPrint(StringBuilder sb)
    {
      sb.AppendLine("Expand");
      Input.PrettyPrint(sb);
    }
  }
}
