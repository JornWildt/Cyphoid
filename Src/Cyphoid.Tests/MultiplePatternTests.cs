namespace Cyphoid.Tests
{
  [TestFixture]
  internal class MultiplePatternTests : TestHelper
  {
    [OneTimeSetUp]
    public void SetUpGraph()
    {
      foreach (var id in new[] { "A", "B", "C" })
      {
        Graph.AddNode(id, "person");
        Graph.SetNodeProperty(id, "name", id);
      }
      Graph.AddNode("DK", "country");
      Graph.AddNode("NO", "country");
      Graph.AddEdge("A", "B", "knows");
      Graph.AddEdge("B", "C", "knows");
      Graph.AddEdge("A", "DK", "lives_in");
      Graph.AddEdge("B", "NO", "lives_in");
    }

    [TestCase("MATCH (p:person), (c:country) RETURN p, c", 6)]
    [TestCase("MATCH (p:person), (c:country), (q:person) RETURN p, c, q", 18)]
    [TestCase("MATCH (p:person), (p) RETURN p", 3)]
    [TestCase("MATCH (p:person), (p:country) RETURN p", 0)]
    [TestCase("MATCH (p:person), (p {name: 'A'}) RETURN p", 1)]
    [TestCase("MATCH (p:person), (missing:absent) RETURN p, missing", 0)]
    [TestCase("MATCH (missing:absent), (p:person) RETURN p, missing", 0)]
    [TestCase("MATCH (p:person)-[:knows]->(q), (q)-[:knows]->(r) RETURN p, q, r", 1)]
    [TestCase("MATCH (p:person), (c:country) WHERE p.name = 'A' RETURN p, c", 2)]
    [TestCase("MATCH (p:person) WITH p AS p MATCH (p)-[:knows]->(q), (c:country) RETURN p, q, c", 4)]
    public async Task MatchEvaluatesEveryPatternPart(string query, int expectedCount)
    {
      var result = await ExecuteQuery(query);
      Assert.That(result.Print, Is.EqualTo(query.Replace("'", "\"")));
      Assert.That(result.Rows.Count, Is.EqualTo(expectedCount));
      Assert.That(result.Rows.All(r => r.Values.All(v => v != null)), Is.True);
    }

    [Test]
    public async Task IndependentScansPreserveBothBindings()
    {
      var result = await ExecuteQuery("MATCH (p:person), (c:country) RETURN id(p) AS person, id(c) AS country");
      var expected = from person in new[] { "A", "B", "C" }
                     from country in new[] { "DK", "NO" }
                     select (person, country);
      Assert.That(result.Rows.Select(r => ((string)r["person"]!, (string)r["country"]!)), Is.EquivalentTo(expected));
    }

    [Test]
    public async Task NewPartCanConnectToVariableBoundByEarlierPart()
    {
      var result = await ExecuteQuery("MATCH (c:country), (p:person)-[:lives_in]->(c) RETURN id(p) AS person, id(c) AS country");
      Assert.That(result.Rows.Select(r => ((string)r["person"]!, (string)r["country"]!)),
        Is.EquivalentTo(new[] { ("A", "DK"), ("B", "NO") }));
    }

    [Test]
    public async Task OptionalMatchNullsAllNewPartsWhenAnyPartFails()
    {
      var result = await ExecuteQuery("MATCH (p:person) OPTIONAL MATCH (p)-[:knows]->(q), (missing:absent) RETURN id(p) AS person, q, missing");
      Assert.That(result.Rows.Count, Is.EqualTo(3));
      Assert.That(result.Rows.Select(r => r["person"]), Is.EquivalentTo(new[] { "A", "B", "C" }));
      Assert.That(result.Rows.All(r => r["q"] == null && r["missing"] == null), Is.True);
    }

    [Test]
    public async Task OptionalMatchReturnsCompleteCombinationsOrOneNullFallback()
    {
      var result = await ExecuteQuery("MATCH (p:person) OPTIONAL MATCH (p)-[:knows]->(q), (c:country) RETURN id(p) AS person, id(q) AS friend, id(c) AS country");
      Assert.That(result.Rows.Count, Is.EqualTo(5));
      Assert.That(result.Rows.Count(r => Equals(r["person"], "A") && Equals(r["friend"], "B") && r["country"] != null), Is.EqualTo(2));
      Assert.That(result.Rows.Count(r => Equals(r["person"], "B") && Equals(r["friend"], "C") && r["country"] != null), Is.EqualTo(2));
      Assert.That(result.Rows.Single(r => Equals(r["person"], "C"))["friend"], Is.Null);
    }
  }
}
