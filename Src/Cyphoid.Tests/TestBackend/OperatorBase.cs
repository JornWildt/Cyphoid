using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cyphoid.Tests.TestBackend
{
  internal abstract class OperatorBase
  {
    protected InMemoryGraph Graph;


    public OperatorBase(InMemoryGraph graph)
    {
      Graph = graph;
    }

  }
}
