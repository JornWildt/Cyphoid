using Cyphoid.Core;
using Cyphoid.Core.Exceptions;
using Cyphoid.Core.Expressions;
using Cyphoid.Tests.TestBackend;

namespace Cyphoid.Tests;

[TestFixture]
internal class CaseTests : TestHelper
{
  [SetUp]
  public void Setup()
  {
    Graph = new InMemoryGraph();
    foreach (var id in new[] { "p0", "p1", "p2" }) Graph.AddNode(id, "project");
    foreach (var id in new[] { "a1", "a2", "a3" }) Graph.AddNode(id, "activity");
    Graph.AddEdge("a1", "p1", "in_project");
    Graph.AddEdge("a2", "p2", "in_project");
    Graph.AddEdge("a3", "p2", "in_project");
  }

  [TestCase("count(CASE WHEN activity_count > 0 THEN 1 END)", "count(CASE WHEN activity_count = 0 THEN 1 END)")]
  [TestCase("sum(CASE WHEN activity_count > 0 THEN 1 ELSE 0 END)", "sum(CASE WHEN activity_count = 0 THEN 1 ELSE 0 END)")]
  public async Task RequestedConditionalAggregation(string with, string without)
  {
    var rows = await BasicExecuteQuery($"MATCH (p:project) OPTIONAL MATCH (a:activity)-[:in_project]->(p) WITH p, count(a) AS activity_count RETURN {with} AS projects_with_activities, {without} AS projects_without_activities");
    Assert.That(rows.Single().Values, Is.EqualTo(new MixedValue?[] { MixedValue.Int(2), MixedValue.Int(1) }));
  }

  [Test]
  public async Task ExistingFallbacksAndPerProjectCounts()
  {
    var prefix = "MATCH (p:project) OPTIONAL MATCH (a:activity)-[:in_project]->(p) WITH p, count(a) AS activity_count ";
    var rows = await BasicExecuteQuery(prefix + "RETURN activity_count ORDER BY activity_count");
    Assert.That(rows.Select(r => r.Values[0]), Is.EqualTo(new MixedValue?[] { MixedValue.Int(0), MixedValue.Int(1), MixedValue.Int(2) }));
    foreach (var (predicate, count) in new[] { ("> 0", 2), ("= 0", 1) })
      Assert.That((await BasicExecuteQuery(prefix + $"WHERE activity_count {predicate} RETURN count(p)")).Single().Values[0], Is.EqualTo(MixedValue.Int(count)));
  }

  [Test]
  public async Task ExistsAlternativeWorksInsideAggregateCase()
  {
    var rows = await BasicExecuteQuery("MATCH (p:project) RETURN count(CASE WHEN EXISTS { MATCH (p)<-[:in_project]-(:activity) } THEN 1 END) AS present, count(CASE WHEN NOT EXISTS { MATCH (p)<-[:in_project]-(:activity) } THEN 1 END) AS absent");
    Assert.That(rows.Single().Values, Is.EqualTo(new MixedValue?[] { MixedValue.Int(2), MixedValue.Int(1) }));
  }

  [TestCase("CASE WHEN true THEN 1 ELSE 1 / 0 END", 1)]
  [TestCase("CASE WHEN false THEN 1 / 0 WHEN true THEN 2 END", 2)]
  [TestCase("CASE WHEN null THEN 1 ELSE 2 END", 2)]
  [TestCase("CASE 2 WHEN 1 THEN 1 / 0 WHEN 2 THEN 3 ELSE 1 / 0 END", 3)]
  [TestCase("CASE null WHEN null THEN 1 ELSE 2 END", 2)]
  [TestCase("CASE WHEN true THEN CASE 1 WHEN 1 THEN 4 END END", 4)]
  [TestCase("CASE WHEN EXISTS { RETURN 1 } THEN 5 ELSE 1 / 0 END", 5)]
  [TestCase("CASE WHEN false THEN EXISTS { RETURN 1 / 0 } ELSE 6 END", 6)]
  [TestCase("CASE EXISTS { RETURN 1 } WHEN true THEN 7 ELSE 1 / 0 END", 7)]
  public async Task CaseEvaluationAndLaziness(string expression, long expected) =>
    Assert.That((await BasicExecuteQuery($"RETURN {expression} AS value")).Single().Values[0], Is.EqualTo(MixedValue.Int(expected)));

  [Test]
  public async Task OmittedElseAndEmptyInput()
  {
    Assert.That((await BasicExecuteQuery("RETURN CASE WHEN false THEN 1 END AS value")).Single().Values[0]!.Value.IsNull(), Is.True);
    Assert.That((await BasicExecuteQuery("MATCH (p:missing) RETURN count(CASE WHEN true THEN 1 END) AS count")).Single().Values[0], Is.EqualTo(MixedValue.Int(0)));
  }

  [TestCase("RETURN CASE WHEN 1 THEN 2 END")]
  public void InvalidCondition(string query) => Assert.ThrowsAsync<RuntimeException>(async () => await BasicExecuteQuery(query));

  [TestCase("RETURN CASE WHEN true THEN count(*) END")]
  [TestCase("RETURN CASE END")]
  public void RejectUnsupportedOrInvalidCase(string query) =>
    Assert.Throws<ParseException>(() => ((ICypherParser)new CypherAstParser()).ParseQuery(query));

  [Test]
  public void PrettyPrintAndParameters()
  {
    const string query = "RETURN CASE $value WHEN 1 THEN $first ELSE $other END AS result";
    var parsed = ((ICypherParser)new CypherAstParser()).ParseQuery(query);
    Assert.That(parsed.PrettyPrint(), Is.EqualTo(query));
    Assert.That(parsed.RequiredParameters, Is.EqualTo(new[] { "value", "first", "other" }));
  }
}
