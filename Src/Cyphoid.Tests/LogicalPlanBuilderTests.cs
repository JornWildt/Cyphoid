using Cyphoid.Core;

namespace Cyphoid.Tests
{
  [TestFixture]
  internal class LogicalPlanBuilderTests
  {
    [TestCase("MATCH (n) RETURN n", "Project: n\r\nNodescan (n)")]
    [TestCase("MATCH (n) WHERE n AND NOT n RETURN n", "Project: n\r\nFilter: n AND NOT n\r\nNodescan (n)")]
    [TestCase("MATCH (n {Category: \"Solar\"}) RETURN n", "Project: n\r\nNodescan (n {Category: \"Solar\"})")]
    public void ItCanParseAndGeneratePlan(string input, string expectedOutput)
    {
      // Arrange
      ICypherParser parser = new CypherAstParser();

      // Act
      var queryNode = parser.ParseQuery(input);
      var plan = queryNode.BuildQueryPlan();
      var output = plan.PrettyPrint();

      Console.WriteLine("***  " + input);
      Console.WriteLine(output);

      // Assert
      Assert.That(output.Trim(), Is.EqualTo(expectedOutput));
    }
  }
}
