using Cyphoid.Core;
using Cyphoid.Core.Exceptions;

namespace Cyphoid.Tests
{
  [TestFixture]
  internal class LabelPredicateTests : TestHelper
  {
    [OneTimeSetUp]
    public void SetUpGraph()
    {
      Graph.AddNode("species", "species");
      Graph.SetNodeProperty("species", "id", "675f7050-80ff-4633-8bb8-e0cf88939536");
      foreach (var label in new[] { "pressure", "activity", "impact", "other" })
      {
        Graph.AddNode(label, label);
        Graph.AddEdge(label, "species", "affects");
      }
    }

    [TestCase("n:pressure", new[] { "pressure" })]
    [TestCase("n:pressure OR n:activity OR n:impact", new[] { "pressure", "activity", "impact" })]
    [TestCase("NOT n:pressure AND n:impact", new[] { "impact" })]
    [TestCase("(n:pressure OR n:activity) AND NOT n:impact", new[] { "pressure", "activity" })]
    [TestCase("n:pressure = true", new[] { "pressure" })]
    [TestCase("n:Pressure", new string[] { })]
    public async Task LabelPredicatesFilterBySingleNodeType(string predicate, string[] expected)
    {
      var query = $"MATCH (n) WHERE {predicate} RETURN id(n) AS id";
      var result = await ExecuteQuery(query);
      Assert.That(result.Print, Is.EqualTo(query));
      Assert.That(result.Rows.Select(r => r["id"]), Is.EquivalentTo(expected));
    }

    [Test]
    public async Task LabelPredicatesCanBeProjectedAndUsedAfterWith()
    {
      var result = await ExecuteQuery("MATCH (n:pressure) WITH n AS p WHERE p:pressure RETURN p:pressure AS matches, p:impact AS other");
      Assert.That(result.Rows.Count, Is.EqualTo(1));
      Assert.That(result.Rows[0]["matches"], Is.EqualTo(true));
      Assert.That(result.Rows[0]["other"], Is.EqualTo(false));
    }

    [Test]
    public async Task OptionalMatchFiltersCandidateLabels()
    {
      const string query = "MATCH (s:species) WHERE s.id = '675f7050-80ff-4633-8bb8-e0cf88939536' OPTIONAL MATCH (n)-[:affects]->(s) WHERE n:pressure OR n:activity OR n:impact RETURN id(n) AS id";
      var result = await ExecuteQuery(query);
      Assert.That(result.Rows.Select(r => r["id"]), Is.EquivalentTo(new[] { "pressure", "activity", "impact" }));
    }

    [Test]
    public async Task OptionalMatchPreservesSpeciesWhenNoLabelMatches()
    {
      var result = await ExecuteQuery("MATCH (s:species) OPTIONAL MATCH (n)-[:affects]->(s) WHERE n:missing RETURN id(s) AS species, n:pressure AS matches");
      Assert.That(result.Rows.Count, Is.EqualTo(1));
      Assert.That(result.Rows[0]["species"], Is.EqualTo("species"));
      Assert.That(result.Rows[0]["matches"], Is.Null);
    }

    [Test]
    public async Task NullVariableProducesNullLabelPredicate()
    {
      var result = await ExecuteQuery("WITH null AS n RETURN n:pressure AS matches");
      Assert.That(result.Rows[0]["matches"], Is.Null);
    }

    [Test]
    public void LabelPredicatesRejectUnknownVariables()
    {
      ICypherParser parser = new CypherAstParser();
      Assert.Throws<ParseException>(() => parser.ParseQuery("MATCH (n) WHERE missing:pressure RETURN n"));
    }

    [Test]
    public void LabelPredicatesRejectNonNodeValues()
    {
      var error = Assert.CatchAsync<Exception>(async () => await ExecuteQuery("WITH 1 AS n RETURN n:pressure AS matches"));
      Assert.That(error!.GetType().Name, Is.EqualTo("RuntimeException"));
    }
  }
}
