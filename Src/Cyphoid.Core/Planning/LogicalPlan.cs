using System.Text;
using Cyphoid.Core.Execution;

namespace Cyphoid.Core.Planning
{
  public record NodeVariable(string Name, int SlotIndex);

  public abstract record LogicalPlan()
  {
    public abstract IOperator BuildExecutionPlan(IOperatorFactory factory);

    public virtual string PrettyPrint()
    {
      StringBuilder sb = new StringBuilder();
      PrettyPrint(sb);
      return sb.ToString();
    }

    public abstract void PrettyPrint(StringBuilder sb);
  }
}
