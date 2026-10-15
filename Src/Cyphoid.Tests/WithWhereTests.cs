using Cyphoid.Core;
using Cyphoid.Core.Exceptions;

namespace Cyphoid.Tests
{
  [TestFixture]
  internal class WithWhereTests : TestHelper
  {
    [OneTimeSetUp]
    public void SetUpGraph()
    {
      foreach (var (id, country, score) in new[] { ("A", "DK", 1), ("B", "DK", 2), ("C", "NO", 3) })
      {
        Graph.AddNode(id, "person");
        Graph.SetNodeProperty(id, "country", country);
        Graph.SetNodeProperty(id, "score", score);
      }
    }

    [TestCase("MATCH (p:person) WITH p.score AS score WHERE score > 1 RETURN score ORDER BY score", new long[] { 2, 3 })]
    [TestCase("MATCH (p:person) WITH p.score AS score ORDER BY score LIMIT 2 WHERE score > 1 RETURN score", new long[] { 2 })]
    [TestCase("MATCH (p:person) WITH p.score AS score ORDER BY score DESC WHERE score > 1 RETURN score", new long[] { 3, 2 })]
    [TestCase("MATCH (p:person) WITH p.score AS score WHERE score > 1 WITH score WHERE score < 3 RETURN score", new long[] { 2 })]
    public async Task WithWhereFiltersProjectedValues(string query, long[] expected)
    {
      var result = await ExecuteQuery(query);
      Assert.That(result.Print, Is.EqualTo(query));
      Assert.That(result.Rows.Select(r => r["score"]), Is.EqualTo(expected));
    }

    [TestCase(1, new[] { "DK" })]
    [TestCase(10, new string[] { })]
    public async Task WithWhereFiltersAggregateResults(int threshold, string[] expected)
    {
      var query = $"MATCH (p:person) WITH p.country AS country, count(*) AS total WHERE total > {threshold} RETURN country, total";
      var result = await ExecuteQuery(query);
      Assert.That(result.Print, Is.EqualTo(query));
      Assert.That(result.Rows.Select(r => r["country"]), Is.EqualTo(expected));
      if (expected.Length > 0)
        Assert.That(result.Rows[0]["total"], Is.EqualTo(2L));
    }

    [Test]
    public async Task WithWhereCanFilterAndPassNodesIntoFollowingMatch()
    {
      Graph.AddEdge("B", "C", "knows");
      var result = await ExecuteQuery("MATCH (p:person) WITH p WHERE p.score = 2 MATCH (p)-[:knows]->(friend) RETURN id(friend) AS id");
      Assert.That(result.Rows.Select(r => r["id"]), Is.EqualTo(new[] { "C" }));
    }

    [Test]
    public async Task WithWhereRejectsMissingPropertyValues()
    {
      var result = await ExecuteQuery("MATCH (p:person) WITH p.missing AS value WHERE value IS NOT NULL RETURN value");
      Assert.That(result.Rows, Is.Empty);
    }

    [TestCase("MATCH (p) WITH p.score AS score WHERE p.score > 1 RETURN score")]
    [TestCase("MATCH (p) WITH p.country AS country, count(*) AS total WHERE p.score > 1 RETURN total")]
    public void WithWhereRejectsVariablesRemovedByProjection(string query)
    {
      ICypherParser parser = new CypherAstParser();
      Assert.Throws<ParseException>(() => parser.ParseQuery(query));
    }
  }
}
