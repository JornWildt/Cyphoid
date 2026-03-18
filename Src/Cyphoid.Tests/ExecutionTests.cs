using Cyphoid.Core;
using Cyphoid.Core.Execution;
using Cyphoid.Tests.TestBackend;

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

      Graph.SetNodeProperty("Copenhagen", "name", "København");
      Graph.SetNodeProperty("Copenhagen", "isDanish", true);
      
      Graph.SetNodeProperty("Oslo", "name", "Oslo");
      Graph.SetNodeProperty("Oslo", "isNorwegian", true);
    }


    [TestCase("MATCH (n) RETURN n", 7)]
    [TestCase("MATCH (n:person) RETURN n", 3)]
    [TestCase("MATCH (n:city) RETURN n", 4)]
    [TestCase("MATCH (n:city {name: \"København\"}) RETURN n", 1)]
    [TestCase("MATCH (n:city {name: \"Oslo\"}) RETURN n", 1)]
    [TestCase("MATCH (n:city {name: \"Oslo\", isNorwegian: true}) RETURN n", 1)]
    [TestCase("MATCH (n:city {name: \"Oslo\", isDanish: true}) RETURN n", 0)]
    [TestCase("MATCH (n:city {name: \"Unused\"}) RETURN n", 0)]
    [TestCase("MATCH (n:nothing) RETURN n", 0)]
    public async Task ItCanExecuteQuery(string input, int rowCount)
    {
      // Arrange
      ICypherParser parser = new CypherAstParser();
      IOperatorFactory factory = new OperatorFactory(Graph);

      // Act
      var queryNode = parser.ParseQuery(input);
      var plan = queryNode.BuildQueryPlan();
      var execution = plan.BuildExecutionPlan(factory);
      var context = new QueryContext(queryNode.RowSize);

      var result = execution.ExecuteAsync(context).ToBlockingEnumerable().ToList();

      // Assert
      Assert.That(result.Count, Is.EqualTo(rowCount));
    }
  }
}
