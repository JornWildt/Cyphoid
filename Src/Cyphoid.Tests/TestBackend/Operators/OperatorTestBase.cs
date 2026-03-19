using Cyphoid.Core.Execution;

namespace Cyphoid.Tests.TestBackend.Operators
{
  internal abstract class OperatorTestBase
  {
    protected InMemoryGraph Graph;


    public OperatorTestBase(InMemoryGraph graph)
    {
      Graph = graph;
    }


    protected bool PropertyMatch(PropertyFilter filter, InMemoryGraph.Node node)
    {
      foreach (var p in filter.Conditions)
      {
        if (!node.Properties.TryGetValue(p.PropertyName, out var propertyValue))
          return false;

        // FIXME: Expr must be evaluated to constant value
        if (!p.Value.Equals(propertyValue))
          return false;
      }

      return true;
    }
  }
}
