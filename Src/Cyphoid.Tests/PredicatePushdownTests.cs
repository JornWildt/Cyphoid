using Cyphoid.Core;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;
using Cyphoid.Core.Exceptions;
using Cyphoid.Tests.TestBackend;

namespace Cyphoid.Tests;

[TestFixture]
internal class PredicatePushdownTests
{
  private sealed class RecordingFactory(InMemoryGraph graph) : TestOperatorFactory(graph)
  {
    public List<(string Variable, PropertyFilter? Filter)> Scans { get; } = [];
    public List<(string Variable, PropertyFilter? Filter)> Expansions { get; } = [];
    protected override IOperator BuildNodeScan(VariableDefinition variable, string? label,
      PropertyFilter? propertyFilter, string? targetRelationshipLabel, RowSchema matchColumns)
    {
      Scans.Add((variable.Name, propertyFilter));
      return base.BuildNodeScan(variable, label, propertyFilter, targetRelationshipLabel, matchColumns);
    }
    protected override IOperator BuildExpandAll(IOperator input, VariableDefinition sourceVariable,
      ExpandDirectionType direction, string? relationLabel, VariableDefinition destinationVariable,
      string? destinationLabel, PropertyFilter? destinationPropertyFilter, RowSchema matchColumns)
    {
      Expansions.Add((destinationVariable.Name, destinationPropertyFilter));
      return base.BuildExpandAll(input, sourceVariable, direction, relationLabel, destinationVariable,
        destinationLabel, destinationPropertyFilter, matchColumns);
    }
  }

  private sealed class Context(IReadOnlyDictionary<string, MixedValue> parameters) : IQueryContext
  {
    public IQueryRowFactory RowFactory => ((IQueryContext)new TestQueryContext()).RowFactory;
    public IReadOnlyDictionary<string, MixedValue> Parameters => parameters;
  }

  private static InMemoryGraph Graph()
  {
    var graph = new InMemoryGraph();
    graph.AddNode("a", "person");
    graph.AddNode("b", "person");
    graph.SetNodeProperty("a", "age", 20L);
    graph.SetNodeProperty("b", "age", 30L);
    graph.AddEdge("a", "b", "LINK");
    return graph;
  }

  private static IOperator Plan(string query, RecordingFactory factory) =>
    ((ICypherParser)new CypherAstParser()).ParseQuery(query).BuildQueryPlan().BuildExecutionPlan(factory);

  [Test]
  public async Task ScanAndExpansionReceiveNamedConstraintsAlongsidePatternConstraints()
  {
    var factory = new RecordingFactory(Graph());
    var rows = await Plan("MATCH (a:person {age: 20})-[:LINK]->(b) WHERE a.age = 20 AND 30 = b.age RETURN b.age AS age", factory)
      .ExecuteAsync(new TestQueryContext()).ToListAsync();
    Assert.That(rows.Single().Values[0], Is.EqualTo(MixedValue.Int(30)));
    Assert.That(factory.Scans.Single().Filter!.Conditions.Select(c => c.Equality),
      Is.EqualTo(new[] { PropertyEquality.Pattern, PropertyEquality.Query }));
    var constraint = factory.Expansions.Single().Filter!.Conditions.Single();
    Assert.That(constraint.PropertyName, Is.EqualTo("age"));
    Assert.That(constraint.Value, Is.EqualTo(MixedValue.Int(30)));
  }

  [Test]
  public async Task ParametersAreBoundAgainForEachExecution()
  {
    var factory = new RecordingFactory(Graph());
    var plan = Plan("MATCH (a) WHERE a.age = $age RETURN a.age AS age", factory);
    foreach (var age in new[] { 20, 30 })
    {
      var rows = await plan.ExecuteAsync(new Context(new QueryParameters(
        new Dictionary<string, MixedValue> { ["age"] = MixedValue.Int(age) }))).ToListAsync();
      Assert.That(rows.Single().Values[0], Is.EqualTo(MixedValue.Int(age)));
      Assert.That(factory.Scans.Last().Filter!.Conditions.Single().Value, Is.EqualTo(MixedValue.Int(age)));
    }
  }

  [TestCase("rand() < 0.5 AND a.age = 20")]
  [TestCase("a.age = 20 OR a.age = 30")]
  [TestCase("EXISTS { MATCH (a)-->(b) } AND a.age = 20")]
  [TestCase("a.age > 10 AND a.age = 20")]
  public async Task UnsupportedLeadingTermsAreOptimizationBarriers(string predicate)
  {
    var factory = new RecordingFactory(Graph());
    await Plan($"MATCH (a) WHERE {predicate} RETURN a", factory).ExecuteAsync(new TestQueryContext()).ToListAsync();
    Assert.That(factory.Scans.All(s => s.Filter == null), Is.True);
  }

  [Test]
  public void PushdownCannotHideAnEarlierError()
  {
    var factory = new RecordingFactory(Graph());
    var plan = Plan("MATCH (a) WHERE 1 / 0 = 1 AND a.age = 999 RETURN a", factory);
    Assert.ThrowsAsync<RuntimeException>(async () => await plan.ExecuteAsync(new TestQueryContext()).ToListAsync());
    Assert.That(factory.Scans.Single().Filter, Is.Null);
  }

  [Test]
  public async Task IndependentPatternPartsFilterTheirOwnScans()
  {
    var factory = new RecordingFactory(Graph());
    var rows = await Plan("MATCH (a), (b) WHERE a.age = 20 AND b.age = 30 RETURN a, b", factory)
      .ExecuteAsync(new TestQueryContext()).ToListAsync();
    Assert.That(rows, Has.Count.EqualTo(1));
    Assert.That(factory.Scans.Select(s => s.Filter!.Conditions.Single().Value),
      Is.EquivalentTo(new[] { MixedValue.Int(20), MixedValue.Int(30) }));
  }

  [Test]
  public async Task OptionalMatchKeepsRejectedOuterRows()
  {
    var factory = new RecordingFactory(Graph());
    var rows = await Plan("MATCH (a) OPTIONAL MATCH (a)-[:LINK]->(b) WHERE a.age = 999 RETURN a, b", factory)
      .ExecuteAsync(new TestQueryContext()).ToListAsync();
    Assert.That(rows, Has.Count.EqualTo(2));
    Assert.That(rows.All(r => r.Values[1]!.Value.IsNull()), Is.True);
    Assert.That(factory.Scans.Single().Filter, Is.Null);
  }

  [Test]
  public async Task OptionalDestinationConstraintRetainsNullFallback()
  {
    var factory = new RecordingFactory(Graph());
    var rows = await Plan("MATCH (a {age: 20}) OPTIONAL MATCH (a)-[:LINK]->(b) WHERE b.age = 999 RETURN b", factory)
      .ExecuteAsync(new TestQueryContext()).ToListAsync();
    Assert.That(rows.Single().Values[0]!.Value.IsNull(), Is.True);
    Assert.That(factory.Expansions.Single().Filter!.Conditions.Single().Value, Is.EqualTo(MixedValue.Int(999)));
  }

  [Test]
  public async Task MissingPropertyUsesQueryEqualityRatherThanPatternEquality()
  {
    var factory = new RecordingFactory(Graph());
    var rows = await Plan("MATCH (a) WHERE a.missing = null RETURN a", factory)
      .ExecuteAsync(new TestQueryContext()).ToListAsync();
    // Match the current runtime's scalar equality semantics, even where they differ
    // from pattern-map matching. Pushdown must not silently change language behavior.
    Assert.That(rows, Has.Count.EqualTo(2));
  }

  [Test]
  public void NullComparedWithMapDoesNotAllowLaterConstraintsToHideErrors()
  {
    var graph = Graph();
    graph.SetNodeProperty("a", "data", new Dictionary<string, object?> { ["x"] = null });
    var factory = new RecordingFactory(graph);
    var plan = Plan("MATCH (a) WHERE a.data = null AND a.age = 999 RETURN a", factory);
    Assert.ThrowsAsync<RuntimeException>(async () => await plan.ExecuteAsync(new TestQueryContext()).ToListAsync());
    Assert.That(factory.Scans.Single().Filter!.Conditions, Has.Count.EqualTo(1));
  }

  [Test]
  public void UnknownMapEqualityStillReachesTheResidualEvaluator()
  {
    var graph = Graph();
    graph.SetNodeProperty("a", "data", new Dictionary<string, object?> { ["x"] = null });
    var factory = new RecordingFactory(graph);
    var plan = Plan("MATCH (a) WHERE a.data = $map AND a.age = 999 RETURN a", factory);
    var context = new Context(new QueryParameters(new Dictionary<string, MixedValue>
    {
      ["map"] = MixedValue.Map(new Dictionary<string, MixedValue> { ["x"] = MixedValue.Null() })
    }));
    Assert.ThrowsAsync<RuntimeException>(async () => await plan.ExecuteAsync(context).ToListAsync());
    Assert.That(factory.Scans.Single().Filter!.Conditions, Has.Count.EqualTo(1));
  }
}
