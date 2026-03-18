using Newtonsoft.Json;

namespace Cyphoid.Tests
{
  [TestFixture]
  internal class RelationshipTests : TestHelper
  {
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
      Graph.AddNode("Lisa", "person");
      Graph.AddNode("Mona", "person");
      Graph.AddNode("Martin", "person");
      Graph.AddNode("Kalle", "person");

      Graph.SetNodeProperty("Lisa", "name", "Lisa Nilson");
      Graph.SetNodeProperty("Mona", "name", "Mona Marius");
      Graph.SetNodeProperty("Martin", "name", "Martin Mollerup");
      Graph.SetNodeProperty("Kalle", "name", "Kalle Karlson");

      Graph.AddNode("Denmark", "country");
      Graph.AddNode("Norway", "country");
      Graph.AddNode("Sweeden", "country");

      Graph.SetNodeProperty("Denmark", "name", "Denmark");
      Graph.SetNodeProperty("Norway", "name", "Norway");
      Graph.SetNodeProperty("Sweeden", "name", "Sweeden");

      Graph.AddEdge("Lisa", "Denmark", "lives_in");
      Graph.AddEdge("Mona", "Denmark", "lives_in");
      Graph.AddEdge("Martin", "Norway", "lives_in");
      Graph.AddEdge("Kalle", "Sweeden", "lives_in");

      Graph.AddEdge("Kalle", "Lisa", "knows");
    }


    [TestCase("MATCH ()-[:nothing]->(:country) RETURN 1", 0)]
    [TestCase("MATCH ()-[:lives_in]->(:country) RETURN 1", 4)]
    [TestCase("MATCH (p:person)-[:lives_in]->(:country) RETURN 1", 4)]
    [TestCase("MATCH (p:person)-[:knows]->(f:person)-[:lives_in]->(:country {name: 'Denmark'}) RETURN 1", 1)]
    [TestCase("MATCH (p:person)-[:lives_in]->(:country {name: 'Norway'}) RETURN 1", 1)]
    [TestCase("MATCH (p:person)<-[:lives_in]-(:country) RETURN 1", 0)]
    [TestCase("MATCH (:country)<-[:lives_in]-(p:person) RETURN 1", 4)]
    [TestCase("MATCH (:country)<-[:nothing]-(p:person) RETURN 1", 0)]
    public async Task ItCanExpandBothDirections(string input, int rowCount)
    {
      // Act
      var result = await ExecuteQuery(input);

      // Assert
      Assert.That(result.Print, Is.EqualTo(input.Replace("'", "\"")));
      Assert.That(result.Rows.Count, Is.EqualTo(rowCount));
    }


    [TestCase("MATCH ()-[r:knows]->() RETURN r AS r", 1)]
    public async Task ItDoesNotCrashOnRelationshipVariables(string input, int rowCount)
    {
      // Act
      var result = await ExecuteQuery(input);

      // Assert
      Assert.That(result.Print, Is.EqualTo(input.Replace("'", "\"")));
      Assert.That(result.Rows.Count, Is.EqualTo(rowCount));

      // - so far undefined, not handling edge variables, but neither breaking.
      Assert.That(result.Rows[0]["r"], Is.Not.Null);
    }


    [TestCase(
      "MATCH (p:person)-[:lives_in]->(:country) WHERE p.name = 'Lisa Nilson' RETURN p.name AS Navn",
      "[{\"Navn\":\"Lisa Nilson\"}]")]
    [TestCase(
      "MATCH (p:person)-[:knows]->(f:person)-[:lives_in]->(:country) RETURN p.name AS Ven1, f.name AS Ven2",
      "[{\"Ven1\":\"Kalle Karlson\",\"Ven2\":\"Lisa Nilson\"}]")]
    public async Task ItCanExpandAndProject(string input, string expectedOutputJson)
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
