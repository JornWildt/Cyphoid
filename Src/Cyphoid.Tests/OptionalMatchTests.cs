using Cyphoid.Tests.TestBackend;

namespace Cyphoid.Tests
{
  [TestFixture]
  internal class OptionalMatchTests : TestHelper
  {
    [SetUp]
    public void SetUpGraph()
    {
      Graph = new InMemoryGraph();
      Graph.AddNode("species", "species");
      Graph.SetNodeProperty("species", "common_name", "California tiger Salamander");
      Graph.AddNode("other", "species");
    }

    const string Query = "MATCH (s:species) WHERE s.common_name = 'California tiger Salamander' OPTIONAL MATCH (i:impact)-[:affects]->(s) RETURN i, s LIMIT 50";

    [TestCase(false)]
    [TestCase(true)]
    public async Task NoCompleteMatchPreservesSpeciesWithNullImpact(bool unrelatedImpact)
    {
      if (unrelatedImpact)
      {
        Graph.AddNode("unrelated", "impact");
        Graph.AddEdge("unrelated", "other", "affects");
      }
      var result = await BasicExecuteQuery(Query);
      Assert.That(result.Count, Is.EqualTo(1));
      Assert.That(result[0].Values[0]?.AsObject(), Is.Null);
      Assert.That(result[0].Values[1]!.Value.AsGraphNode().GetId().AsString(), Is.EqualTo("species"));
    }

    [Test]
    public async Task CompleteMatchesExcludeUnrelatedCandidatesAndNullFallback()
    {
      foreach (var id in new[] { "first", "second", "unrelated" })
        Graph.AddNode(id, "impact");
      Graph.AddEdge("first", "species", "affects");
      Graph.AddEdge("second", "species", "affects");
      Graph.AddEdge("unrelated", "other", "affects");
      var result = await BasicExecuteQuery(Query);
      Assert.That(result.Count, Is.EqualTo(2));
      Assert.That(result.Select(r => r.Values[0]!.Value.AsGraphNode().GetId().AsString()),
        Is.EquivalentTo(new[] { "first", "second" }));
    }

    [Test]
    public async Task OptionalWhereParticipatesInMatchingBeforeNullFallback()
    {
      Graph.AddNode("impact", "impact");
      Graph.AddEdge("impact", "species", "affects");
      var result = await ExecuteQuery("MATCH (s:species) OPTIONAL MATCH (i:impact)-[:affects]->(s) WHERE i.allowed = true RETURN id(s) AS species, id(i) AS impact");
      Assert.That(result.Rows.Count, Is.EqualTo(2));
      Assert.That(result.Rows.All(r => r["impact"] == null), Is.True);
      Assert.That(result.Rows.Select(r => r["species"]), Is.EquivalentTo(new[] { "species", "other" }));
    }

    [Test]
    public async Task PartialPatternFailureClearsAllNewVariables()
    {
      Graph.AddNode("impact", "impact");
      Graph.AddEdge("species", "impact", "affected_by");
      var result = await ExecuteQuery("MATCH (s:species) OPTIONAL MATCH (s)-[:affected_by]->(i)-[:caused_by]->(cause) RETURN id(s) AS species, id(i) AS impact, id(cause) AS cause");
      Assert.That(result.Rows.Count, Is.EqualTo(2));
      Assert.That(result.Rows.All(r => r["impact"] == null && r["cause"] == null), Is.True);
    }

    [Test]
    public async Task IncomingExpansionBindsNewNode()
    {
      Graph.AddNode("impact", "impact");
      Graph.AddEdge("impact", "species", "affects");
      var result = await ExecuteQuery("MATCH (s:species) OPTIONAL MATCH (s)<-[:affects]-(i:impact) RETURN id(s) AS species, id(i) AS impact");
      Assert.That(result.Rows.Count, Is.EqualTo(2));
      Assert.That(result.Rows.Single(r => Equals(r["species"], "species"))["impact"], Is.EqualTo("impact"));
      Assert.That(result.Rows.Single(r => Equals(r["species"], "other"))["impact"], Is.Null);
    }

    [Test]
    public async Task InitialOptionalMatchProducesOneNullRowWhenScanIsEmpty()
    {
      var result = await ExecuteQuery("OPTIONAL MATCH (i:impact) RETURN i");
      Assert.That(result.Rows.Count, Is.EqualTo(1));
      Assert.That(result.Rows[0]["i"], Is.Null);
    }

    [Test]
    public async Task EmptyRequiredInputProducesNoOptionalRows()
    {
      var result = await ExecuteQuery("MATCH (s:absent) OPTIONAL MATCH (i:impact) RETURN s, i");
      Assert.That(result.Rows, Is.Empty);
    }
  }
}
