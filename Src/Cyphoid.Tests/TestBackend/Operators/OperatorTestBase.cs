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
        var exists = node.Properties.TryGetValue(p.PropertyName, out var propertyValue);
        if (!p.IsMatch(exists, Cyphoid.Core.Expressions.MixedValue.FromObject(propertyValue)))
          return false;
      }

      return true;
    }
  }
}
