using Cyphoid.Core;
using Cyphoid.Core.Execution;
using Cyphoid.Tests.TestBackend;

namespace Cyphoid.Tests
{
  internal class TestHelper
  {
    protected InMemoryGraph Graph = new InMemoryGraph();


    protected async Task<(string Print, IReadOnlyList<IDictionary<string, object?>> Rows)> ExecuteQuery(string input)
    {
      var result = await RunQuery(input);

      var output = result.Rows
        .Select(r => r.Values.Select((v, i) => (v, i)).ToDictionary(v => r.Columns[v.i].Name, v => v.v?.AsObject()))
        .ToArray();

      return (result.Print, output);
    }


    protected async Task<List<IRow>> BasicExecuteQuery(string input)
    {
      var result = await RunQuery(input);
      return result.Rows;
    }


    async Task<(string Print, List<IRow> Rows)> RunQuery(string input)
    {
      ICypherParser parser = new CypherAstParser();
      IOperatorFactory factory = new TestOperatorFactory(Graph);

      var queryNode = parser.ParseQuery(input);
      var prettyPrint = queryNode.PrettyPrint();

      var plan = queryNode.BuildQueryPlan();
      var execution = plan.BuildExecutionPlan(factory);
      var context = new TestQueryContext();

      var result = await execution.ExecuteAsync(context).ToListAsync();

      return (prettyPrint, result);
    }
  }
}
