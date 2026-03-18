using Cyphoid.Core;
using Cyphoid.Core.Execution;
using Cyphoid.Tests.TestBackend;

namespace Cyphoid.Tests
{
  internal class TestHelper
  {
    protected InMemoryGraph Graph = new InMemoryGraph();


    protected async Task<(string Print, List<Dictionary<string, object?>> Rows)> ExecuteQuery(string input)
    {
      ICypherParser parser = new CypherAstParser();
      IOperatorFactory factory = new OperatorFactory(Graph);

      var queryNode = parser.ParseQuery(input);
      var prettyPrint = queryNode.PrettyPrint();

      var plan = queryNode.BuildQueryPlan();
      var execution = plan.BuildExecutionPlan(factory);
      var context = new QueryContext(queryNode.RowSize);

      var result = await execution.ExecuteAsync(context).ToListAsync();

      return (prettyPrint, result);
    }
  }
}
