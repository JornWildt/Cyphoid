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
    }


    [TestCase("MATCH (n) RETURN n")]
    public async Task ItCanExecuteQuery(string input)
    {
      // Arrange
      ICypherParser parser = new CypherAstParser();
      IOperatorFactory factory = new TestOperatorFactory(Graph);

      // Act
      var queryNode = parser.ParseQuery(input);
      var plan = queryNode.BuildQueryPlan();
      var execution = plan.BuildExecutionPlan(factory);
      var context = new QueryContext();

      var result = execution.ExecuteAsync(context).ToBlockingEnumerable().ToList();

      // Assert
      Assert.That(result.Count, Is.EqualTo(3));
    }
  }
}
