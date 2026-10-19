using Cyphoid.Core;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Planning;
using Cyphoid.Core.SyntaxTree;
using Cyphoid.Tests.TestBackend;

namespace Cyphoid.Tests
{
  [TestFixture]
  internal class BoundEndpointTests : TestHelper
  {
    [SetUp]
    public void ResetGraph() => Graph = new InMemoryGraph();

    [TestCase("(i:impact)-[:affects]->(s)", RelationshipDirectionType.Left)]
    [TestCase("(i:impact)<-[:affects]-(s)", RelationshipDirectionType.Right)]
    [TestCase("(i:impact)-[:affects]-(s)", RelationshipDirectionType.Unidirectional)]
    public void BoundRightEndpointBecomesExpansionSource(string pattern, RelationshipDirectionType expectedDirection)
    {
      ICypherParser parser = new CypherAstParser();
      var query = parser.ParseQuery($"MATCH (s:species) OPTIONAL MATCH {pattern} RETURN i, s");
      var clause = (MatchWhereNode)query.Clauses[1];
      var plan = clause.MatchPattern.BuildPlan(new EmptyPlan(), clause.DeclaredColumns);

      Assert.That(plan, Is.TypeOf<ExpandAllPlan>());
      var expansion = (ExpandAllPlan)plan;
      Assert.That(expansion.Source.Definition.Name, Is.EqualTo("s"));
      Assert.That(expansion.Destination.Definition.Name, Is.EqualTo("i"));
      Assert.That(expansion.DestinationLabel, Is.EqualTo("impact"));
      Assert.That(expansion.Relationship.RelationshipDirection, Is.EqualTo(expectedDirection));
      Assert.That(expansion.Input, Is.TypeOf<ExpandIntoPlan>());
      Assert.That(((ExpandIntoPlan)expansion.Input).Input, Is.TypeOf<EmptyPlan>());
      Assert.That(query.PrettyPrint(), Is.EqualTo($"MATCH (s:species) OPTIONAL MATCH {pattern} RETURN i, s"));
    }

    [TestCase("(i:impact)-[:affects]->(s)", false)]
    [TestCase("(i:impact)<-[:affects]-(s)", true)]
    [TestCase("(i:impact)-[:affects]-(s)", false)]
    public async Task ReversedExpansionFetchesImpactsWithoutScanningThem(string pattern, bool reverseEdge)
    {
      Graph.AddNode("species", "species");
      Graph.AddNode("impact", "impact");
      Graph.AddEdge(reverseEdge ? "species" : "impact", reverseEdge ? "impact" : "species", "affects");
      ICypherParser parser = new CypherAstParser();
      var factory = new NoImpactScanFactory(Graph);
      var execution = parser.ParseQuery($"MATCH (s:species) OPTIONAL MATCH {pattern} RETURN id(i) AS impact")
        .BuildQueryPlan().BuildExecutionPlan(factory);
      var rows = await execution.ExecuteAsync(new TestQueryContext()).ToListAsync();
      Assert.That(rows.Count, Is.EqualTo(1));
      Assert.That(rows[0].Values[0]!.Value.AsString(), Is.EqualTo("impact"));
    }

    [TestCase("(i:impact {allowed: true})-[:affects]->(s)", false)]
    [TestCase("(i:impact)-[:affects]->(s:wrong)", false)]
    [TestCase("(i:impact)-[:affects]->(s {allowed: true})", false)]
    [TestCase("(i:impact)-[:affects]->(s)", true)]
    public async Task ReversedExpansionPreservesConstraintsAndOptionalFallback(string pattern, bool matches)
    {
      Graph.AddNode("species", "species");
      Graph.AddNode("impact", "impact");
      Graph.AddEdge("impact", "species", "affects");
      var result = await ExecuteQuery($"MATCH (s:species) OPTIONAL MATCH {pattern} RETURN id(s) AS species, id(i) AS impact");
      Assert.That(result.Rows.Count, Is.EqualTo(1));
      Assert.That(result.Rows[0]["species"], Is.EqualTo("species"));
      Assert.That(result.Rows[0]["impact"], Is.EqualTo(matches ? "impact" : null));
    }

    private class NoImpactScanFactory(InMemoryGraph graph) : TestOperatorFactory(graph)
    {
      protected override IOperator BuildNodeScan(VariableDefinition variable, string? label,
        PropertyFilter? propertyFilter, string? targetRelationshipType, RowSchema matchColumns)
      {
        Assert.That(label, Is.Not.EqualTo("impact"), "Bound-endpoint expansion must not scan impacts.");
        return base.BuildNodeScan(variable, label, propertyFilter, targetRelationshipType, matchColumns);
      }
    }
  }
}
