using Newtonsoft.Json;

namespace Cyphoid.Tests
{
  [TestFixture]
  internal class NodeTypeExpressionTests : TestHelper
  {
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
      Graph.AddNode("Copenhagen", "city");
      Graph.AddNode("Oslo", "city");
      Graph.AddNode("Stockholm", "city");
      Graph.AddNode("Helsinki", "city");

      Graph.AddNode("Denmark", "country");
      Graph.AddNode("Norway", "country");
      Graph.AddNode("Germany", "country");

      Graph.SetNodeProperty("Copenhagen", "name", "København");
      Graph.SetNodeProperty("Copenhagen", "isDanish", true);
      Graph.SetNodeProperty("Copenhagen", "population", 1000000);
      Graph.AddEdge("Copenhagen", "Denmark", "located_in");

      Graph.SetNodeProperty("Oslo", "name", "Oslo");
      Graph.AddEdge("Oslo", "Norway", "located_in");

      Graph.SetNodeProperty("Denmark", "name", "Denmark");
      Graph.SetNodeProperty("Norway", "name", "Norway");
      Graph.SetNodeProperty("Germany", "name", "Germany");
    }


    [TestCase(
      "MATCH (n:country) WHERE n.name = 'Germany' RETURN n LIMIT 1",
      "[{\"n\":{\"Id\":\"Germany\",\"Edges\":{},\"Attributes\":{\"name\":\"Germany\"}}}]")]
    public async Task ItCanReturnFullNodeData(string input, string expectedOutputJson)
    {
      // Act
      var result = await ExecuteQuery(input);
      var resultJson = JsonConvert.SerializeObject(result.Rows);

      // Assert
      Assert.That(result.Print, Is.EqualTo(input.Replace("'", "\"")));
      Assert.That(resultJson, Is.EqualTo(expectedOutputJson));
    }
  }
}
