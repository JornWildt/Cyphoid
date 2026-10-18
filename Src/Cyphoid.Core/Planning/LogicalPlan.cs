using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning
{
  public abstract record LogicalPlan()
  {
    public virtual string PrettyPrint()
    {
      StringBuilder sb = new StringBuilder();
      PrettyPrint(sb);
      return sb.ToString();
    }

    public abstract void PrettyPrint(StringBuilder sb);


    // WHERE constraints remain distinct from pattern-map equalities: their null
    // and nested-map equality rules differ. Values are bound once per execution,
    // never evaluated against an incomplete scan row.
    protected PropertyFilter? BuildPropertyFilter(PropertyMapNode? map, QueryParameters parameters,
      IReadOnlyList<PropertyEntryNode> constraints)
    {
      var pattern = BuildPropertyFilter(map, parameters);
      if (constraints.Count == 0) return pattern;
      return new PropertyFilter((pattern?.Conditions ?? []).Concat(constraints.Select(c =>
        new PropertyFilterCondition(c.Identifier, ((LiteralValueNode)c.Value.Bind(parameters)).ToConstantValue(),
          PropertyEquality.Query))).ToArray());
    }

    protected PropertyFilter? BuildPropertyFilter(PropertyMapNode? propertyMap, QueryParameters parameters)
    {
      var conditions = propertyMap?
        .Properties?
        .Select(p => new PropertyFilterCondition(p.Identifier, ((LiteralValueNode)p.Value.Bind(parameters)).ToConstantValue()))
        .ToList()
        .AsReadOnly();

      var filter = conditions != null ? new PropertyFilter(conditions) : null;
      return filter;
    }
  }
}
