using Cyphoid.Core;
using Cyphoid.Core.Exceptions;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;
using Cyphoid.Tests.TestBackend;

namespace Cyphoid.Tests;

[TestFixture]
internal class NullSemanticsTests : TestHelper
{
  [SetUp]
  public void ResetGraph() => Graph = new InMemoryGraph();

  private async Task<MixedValue> Evaluate(string expression) =>
    (await BasicExecuteQuery($"RETURN {expression} AS value")).Single().Values[0]!.Value;

  public static IEnumerable<TestCaseData> BooleanCases()
  {
    string[] values = ["false", "true", "null"];
    string[,] and = { { "false", "false", "false" }, { "false", "true", "null" }, { "false", "null", "null" } };
    string[,] or = { { "false", "true", "null" }, { "true", "true", "true" }, { "null", "true", "null" } };
    for (var i = 0; i < 3; i++)
      for (var j = 0; j < 3; j++)
        foreach (var async in new[] { false, true })
        {
          string Operand(string value) => !async ? value : value switch
          {
            "true" => "EXISTS { RETURN 1 }",
            "false" => "EXISTS { RETURN 1 LIMIT 0 }",
            _ => "(null AND EXISTS { RETURN 1 })"
          };
          yield return new TestCaseData($"({Operand(values[i])}) AND ({Operand(values[j])})", and[i,j]);
          yield return new TestCaseData($"({Operand(values[i])}) OR ({Operand(values[j])})", or[i,j]);
        }
  }

  [TestCaseSource(nameof(BooleanCases))]
  public async Task BooleanTruthTables(string expression, string expected)
  {
    Assert.That(await Evaluate(expression), Is.EqualTo(expected == "null" ? MixedValue.Null() : MixedValue.Bool(expected == "true")));
  }

  [TestCase("null = null")]
  [TestCase("null <> 1")]
  [TestCase("1 = null")]
  [TestCase("null < 1")]
  [TestCase("1 <= null")]
  [TestCase("null > 1")]
  [TestCase("1 >= null")]
  [TestCase("NOT null")]
  [TestCase("NOT (null AND EXISTS { RETURN 1 })")]
  [TestCase("null IN [null]")]
  [TestCase("null IN [1]")]
  [TestCase("1 IN [null, 2]")]
  [TestCase("null IN [EXISTS { RETURN 1 }]")]
  [TestCase("null + 1")]
  [TestCase("null CONTAINS 'x'")]
  public async Task UnknownResultsPropagate(string expression) => Assert.That((await Evaluate(expression)).IsNull(), Is.True);

  [TestCase("null IN []", false)]
  [TestCase("1 IN [null, 1]", true)]
  [TestCase("true IN [null, EXISTS { RETURN 1 }]", true)]
  [TestCase("NOT false", true)]
  [TestCase("NOT true", false)]
  [TestCase("false AND (1 / 0 = 1)", false)]
  [TestCase("true OR (1 / 0 = 1)", true)]
  [TestCase("false AND EXISTS { RETURN 1 / 0 }", false)]
  [TestCase("true OR EXISTS { RETURN 1 / 0 }", true)]
  public async Task DefiniteResultsAndShortCircuiting(string expression, bool expected) =>
    Assert.That(await Evaluate(expression), Is.EqualTo(MixedValue.Bool(expected)));

  [TestCase("null AND (1 / 0 = 1)")]
  [TestCase("null OR (1 / 0 = 1)")]
  [TestCase("null AND EXISTS { RETURN 1 / 0 }")]
  [TestCase("null OR EXISTS { RETURN 1 / 0 }")]
  [TestCase("NOT 1")]
  [TestCase("null AND 1")]
  public void UnknownDoesNotSkipRequiredEvaluationOrAcceptInvalidBooleans(string expression) =>
    Assert.ThrowsAsync<RuntimeException>(async () => await Evaluate(expression));

  [Test]
  public async Task OptionalMissingPropertiesAndIsNull()
  {
    Graph.AddNode("a", "person");
    var rows = await BasicExecuteQuery("MATCH (a) OPTIONAL MATCH (a)-->(b) RETURN b = null AS eq, b IS NULL AS absent, NOT b.active AS negated");
    Assert.That(rows.Single().Values.Select(v => v!.Value),
      Is.EqualTo(new[] { MixedValue.Null(), MixedValue.Bool(true), MixedValue.Null() }));
    Assert.That(await BasicExecuteQuery("MATCH (a) WHERE a.missing = null RETURN a"), Is.Empty);
    Assert.That(await BasicExecuteQuery("MATCH (a) WHERE a.missing IS NULL RETURN a"), Has.Count.EqualTo(1));
  }

  [TestCase("a.missing = 1")]
  [TestCase("a.missing < 1")]
  public void CandidateUnknownMustNotHideLaterErrors(string predicate)
  {
    Graph.AddNode("a", "person");
    Assert.ThrowsAsync<RuntimeException>(async () =>
      await BasicExecuteQuery($"MATCH (a) WHERE {predicate} AND 1 / 0 = 1 RETURN a"));
  }

  [TestCase("b.missing = 1")]
  [TestCase("b.missing >= 1")]
  public void ExpansionUnknownMustNotHideLaterErrors(string predicate)
  {
    Graph.AddNode("a", "person");
    Graph.AddNode("b", "person");
    Graph.AddEdge("a", "b", "LINK");
    Assert.ThrowsAsync<RuntimeException>(async () => await BasicExecuteQuery(
      $"MATCH (a)-[:LINK]->(b) WHERE {predicate} AND 1 / 0 = 1 RETURN b"));
  }

  [Test]
  public async Task GroupingAndDistinctStillTreatNullsAsOneKey()
  {
    Graph.AddNode("a", "person");
    Graph.AddNode("b", "person");
    var rows = await BasicExecuteQuery("MATCH (a) RETURN a.missing AS key, count(*) AS total");
    Assert.That(rows.Single().Values[1], Is.EqualTo(MixedValue.Int(2)));
    Assert.That(await BasicExecuteQuery("MATCH (a) RETURN DISTINCT a.missing AS key"), Has.Count.EqualTo(1));
  }
}
