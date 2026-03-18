using Cyphoid.Core;
using Cyphoid.Core.Execution;
using Cyphoid.Tests.TestBackend;
using Newtonsoft.Json;
using System.Linq;

namespace Cyphoid.Tests
{
  [TestFixture]
  internal class ExecutionTests
  {
    InMemoryGraph Graph;


    public ExecutionTests()
    {
      Graph = new InMemoryGraph();
    }


    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
      Graph.AddNode("Lisa", "person");
      Graph.AddNode("Mona", "person");
      Graph.AddNode("Martin", "person");

      Graph.SetNodeProperty("Lisa", "Name", "Lisa Nilson");
      Graph.SetNodeProperty("Mona", "Name", "Mona Marius");
      Graph.SetNodeProperty("Martin", "Name", "Martin Mollerup");

      Graph.AddNode("Copenhagen", "city");
      Graph.AddNode("Oslo", "city");
      Graph.AddNode("Stockholm", "city");
      Graph.AddNode("Helsinki", "city");

      Graph.AddNode("Denmark", "country");
      Graph.AddNode("Norway", "country");
      Graph.AddNode("Sweden", "country");
      Graph.AddNode("Germany", "country");
      Graph.AddNode("England", "country");

      Graph.SetNodeProperty("Copenhagen", "name", "København");
      Graph.SetNodeProperty("Copenhagen", "isDanish", true);
      Graph.SetNodeProperty("Copenhagen", "population", 1000000);
      Graph.AddEdge("Copenhagen", "Denmark", "located_in");

      Graph.SetNodeProperty("Oslo", "name", "Oslo");
      Graph.SetNodeProperty("Oslo", "isNorwegian", true);
      Graph.AddEdge("Oslo", "Norway", "located_in");

      Graph.SetNodeProperty("Denmark", "name", "Denmark");

      Graph.SetNodeProperty("Norway", "name", "Norway");
    }


    [TestCase("RETURN 1", 1)]
    [TestCase("MATCH (n) RETURN n", 12)]
    [TestCase("MATCH (n:person) RETURN n", 3)]
    [TestCase("MATCH (n:city) RETURN n", 4)]
    [TestCase("MATCH (n:city {name: \"København\"}) RETURN n", 1)]
    [TestCase("MATCH (n:city {name: \"Oslo\"}) RETURN n", 1)]
    [TestCase("MATCH (n:city {name: \"Oslo\", isNorwegian: true}) RETURN n", 1)]
    [TestCase("MATCH (n:city {name: \"Oslo\", isDanish: true}) RETURN n", 0)]
    [TestCase("MATCH (n:city {name: \"Unused\"}) RETURN n", 0)]
    [TestCase("MATCH (n:nothing) RETURN n", 0)]
    [TestCase("MATCH (n:city)-[]->(c:country) RETURN n", 2)] // Only some cities have been related to a country
    [TestCase("MATCH (n:city)-[]->(c:country {name: \"Norway\"}) RETURN n", 1)]
    [TestCase("MATCH (n:city)-[]->(c:country {name: \"Denmark\"}) RETURN n", 1)]
    [TestCase("MATCH (n:city)-[]->(c:country {name: \"None\"}) RETURN n", 0)]
    [TestCase("MATCH (n:city)-[]->(c:nothing) RETURN n", 0)]
    [TestCase("MATCH (n:nothing)-[]->(c:country) RETURN n", 0)]
    public async Task ItCanExecuteQuery(string input, int rowCount)
    {
      // Act
      var result = await ExecuteQuery(input);

      // Assert
      Assert.That(result.Count, Is.EqualTo(rowCount));
    }


    [TestCase(
      "RETURN 1", 
      "[{\"p1\":1}]")]
    [TestCase(
      "MATCH (n:city) RETURN 1 LIMIT 1",
      "[{\"p1\":1}]")]
    [TestCase(
      "MATCH (n:city) RETURN 1 LIMIT 2",
      "[{\"p1\":1},{\"p1\":1}]")]
    [TestCase(
      "MATCH (n:city {name: \"København\"}) RETURN n.isDanish AS erDansk, n.population AS indbyggere",
      "[{\"erDansk\":true,\"indbyggere\":1000000}]")]
    public async Task ItCanProjectResult(string input, string expectedOutputJson)
    {
      // Act
      var result = await ExecuteQuery(input);
      var resultJson = JsonConvert.SerializeObject(result);

      // Assert
      Assert.That(resultJson, Is.EqualTo(expectedOutputJson));
    }


    protected async Task<List<Dictionary<string, object?>>> ExecuteQuery(string input)
    {
      ICypherParser parser = new CypherAstParser();
      IOperatorFactory factory = new OperatorFactory(Graph);

      var queryNode = parser.ParseQuery(input);
      var plan = queryNode.BuildQueryPlan();
      var execution = plan.BuildExecutionPlan(factory);
      var context = new QueryContext(queryNode.RowSize);

      var result = await execution.ExecuteAsync(context).ToListAsync();

      return result;
    }
  }
}
