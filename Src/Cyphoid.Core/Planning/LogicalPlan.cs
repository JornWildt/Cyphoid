using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning
{
  public abstract record LogicalPlan<TId>() where TId : IEquatable<TId>
  {
    public virtual string PrettyPrint()
    {
      StringBuilder sb = new StringBuilder();
      PrettyPrint(sb);
      return sb.ToString();
    }

    public abstract void PrettyPrint(StringBuilder sb);


    protected PropertyFilter? BuildPropertyFilter(PropertyMapNode? propertyMap)
    {
      var conditions = propertyMap?
        .Properties?
        .Select(p => new PropertyFilterCondition(p.Identifier, p.Value.ToConstantValue()))
        .ToList()
        .AsReadOnly();

      var filter = conditions != null ? new PropertyFilter(conditions) : null;
      return filter;
    }
  }
}
