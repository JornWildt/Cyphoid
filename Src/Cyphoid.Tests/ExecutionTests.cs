using Newtonsoft.Json;

namespace Cyphoid.Tests
{
  [TestFixture]
  internal class ExecutionTests : TestHelper
  {
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
    public async Task ItCanExecuteBasicQuery(string input, int rowCount)
    {
      // Act
      var result = await ExecuteQuery(input);

      // Assert
      Assert.That(result.Print, Is.EqualTo(input.Replace("'", "\"")));
      Assert.That(result.Rows.Count, Is.EqualTo(rowCount));
    }


    [TestCase(
      "RETURN 1",
      "[{\"p1\":1}]")]
    [TestCase(
      "MATCH (n:city) RETURN n.size LIMIT 1",
      "[{\"size\":null}]")]
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
      var resultJson = JsonConvert.SerializeObject(result.Rows);

      // Assert
      Assert.That(result.Print, Is.EqualTo(input.Replace("'", "\"")));
      Assert.That(resultJson, Is.EqualTo(expectedOutputJson));
    }


    [TestCase("MATCH (n) WHERE n.isDanish RETURN n", 1)]
    [TestCase("MATCH (n) WHERE n.isDanish = null RETURN n", 11)]
    [TestCase("MATCH (n) WHERE n.isDanish = true RETURN n", 1)]
    [TestCase("MATCH (n) WHERE n.isDanish = false RETURN n", 0)]
    [TestCase("MATCH (n) WHERE NOT n.isDanish RETURN n", 11)]
    [TestCase("MATCH (n) WHERE n.name = 'Oslo' RETURN n", 1)]
    [TestCase("MATCH (n) WHERE n.name <> 'Oslo' RETURN n", 11)]
    [TestCase("MATCH (n) WHERE n.name = 'Oslo' OR n.name = 'København' RETURN n", 2)]
    [TestCase("MATCH (n) WHERE n.name = 'Oslo' AND n.name = 'København' RETURN n", 0)]
    public async Task ItCanApplyWhere(string input, int rowCount)
    {
      // Act
      var result = await ExecuteQuery(input);

      // Assert
      Assert.That(result.Print, Is.EqualTo(input.Replace("'", "\"")));
      Assert.That(result.Rows.Count, Is.EqualTo(rowCount));
    }


    [TestCase("MATCH (n) WHERE n.name IN ['Oslo', 'København'] RETURN n.name", 2)]
    [TestCase("MATCH (n) WHERE n.name IN [n.name] RETURN n.name", 12)]
    [TestCase("MATCH (n) WHERE n.name IN [] RETURN n.name", 0)]
    public async Task ItCanHandleInOperator(string input, int rowCount)
    {
      // Act
      var result = await ExecuteQuery(input);

      // Assert
      Assert.That(result.Print, Is.EqualTo(input.Replace("'", "\"")));
      Assert.That(result.Rows.Count, Is.EqualTo(rowCount));
    }
  }
}
