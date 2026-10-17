using System.Runtime.CompilerServices;
using Cyphoid.Core;
using Cyphoid.Core.Exceptions;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;
using Cyphoid.Core.Procedures;
using Cyphoid.Tests.TestBackend;

namespace Cyphoid.Tests;

[TestFixture]
internal class ParameterTests
{
  private sealed class Context(IReadOnlyDictionary<string, MixedValue> parameters) : IQueryContext
  {
    private readonly IQueryContext inner = new TestQueryContext();
    public IQueryRowFactory RowFactory => inner.RowFactory;
    public IReadOnlyDictionary<string, MixedValue> Parameters => parameters;
  }

  private static QueryParameters Parameters(params (string Name, MixedValue Value)[] entries) =>
    new(entries.Select(p => new KeyValuePair<string, MixedValue>(p.Name, p.Value)));

  private static InMemoryGraph Graph()
  {
    var graph = new InMemoryGraph();
    graph.AddNode("Alice", "person");
    graph.AddNode("Bob", "person");
    graph.SetNodeProperty("Alice", "age", 20L);
    graph.SetNodeProperty("Bob", "age", 30L);
    graph.AddEdge("Alice", "Bob", "LINK");
    return graph;
  }

  private static IOperator Plan(string query, InMemoryGraph? graph = null)
  {
    ICypherParser parser = new CypherAstParser();
    return parser.ParseQuery(query).BuildQueryPlan().BuildExecutionPlan(new TestOperatorFactory(graph ?? Graph()));
  }

  [TestCase("RETURN $value AS result")]
  [TestCase("WITH $value AS x RETURN x AS result")]
  [TestCase("RETURN coalesce(null, $value) AS result")]
  [TestCase("RETURN {nested: $value}.nested AS result")]
  [TestCase("RETURN {a: 1} {a: $value}.a AS result")]
  public async Task ParametersWorkAcrossExpressionForms(string query)
  {
    var plan = Plan(query);
    var first = await plan.ExecuteAsync(new Context(Parameters(("value", MixedValue.Int(7))))).ToListAsync();
    var second = await plan.ExecuteAsync(new Context(Parameters(("value", MixedValue.String("text"))))).ToListAsync();
    Assert.That(first[0].Values[0], Is.EqualTo(MixedValue.Int(7)));
    Assert.That(second[0].Values[0], Is.EqualTo(MixedValue.String("text")));
    Assert.That(first[0].Schema, Is.SameAs(second[0].Schema));
  }

  [Test]
  public void QueryParametersAreImmutableOrdinalSnapshots()
  {
    var supplied = new Dictionary<string, MixedValue>(StringComparer.OrdinalIgnoreCase) { ["Value"] = MixedValue.Int(1) };
    var parameters = new QueryParameters(supplied);
    supplied.Clear();
    Assert.That(parameters["Value"], Is.EqualTo(MixedValue.Int(1)));
    Assert.That(parameters.ContainsKey("value"), Is.False);
    Assert.That(parameters, Is.Not.InstanceOf<IDictionary<string, MixedValue>>());
    Assert.That(QueryParameters.Empty.Count, Is.Zero);
  }

  [Test]
  public async Task NullPatternParametersDoNotMatchNullProperties()
  {
    var graph = Graph();
    graph.SetNodeProperty("Alice", "age", null);
    var rows = await Plan("MATCH (n:person {age: $age}) RETURN n", graph)
      .ExecuteAsync(new Context(Parameters(("age", MixedValue.Null())))).ToListAsync();
    Assert.That(rows, Is.Empty);
  }

  [Test]
  public async Task ParametersDoNotOccupyRowSchemaSlots()
  {
    var rows = await Plan("WITH $value AS value RETURN value")
      .ExecuteAsync(new Context(Parameters(("value", MixedValue.Int(4))))).ToListAsync();
    Assert.That(rows[0].Schema.Count, Is.EqualTo(1));
    Assert.That(rows[0].Schema[0].Name, Is.EqualTo("value"));
    Assert.That(rows[0].Values[0], Is.EqualTo(MixedValue.Int(4)));
  }

  [Test]
  public async Task MapParametersSupportNestedAndDynamicAccess()
  {
    var config = MixedValue.Map(new Dictionary<string, MixedValue> { ["age"] = MixedValue.Int(25) });
    var rows = await Plan("MATCH (n:person) WHERE n[$key] >= $config.age RETURN id(n) AS person")
      .ExecuteAsync(new Context(Parameters(("key", MixedValue.String("age")), ("config", config)))).ToListAsync();
    Assert.That(rows, Has.Count.EqualTo(1));
    Assert.That(rows[0].Values[0], Is.EqualTo(MixedValue.String("Bob")));
  }

  [TestCase("MATCH (n:person {age: $age}) RETURN id(n) AS person")]
  [TestCase("MATCH (n:person) MATCH (n {age: $age}) RETURN id(n) AS person")]
  [TestCase("MATCH (a:person)-[:LINK]->(n:person {age: $age}) RETURN id(n) AS person")]
  [TestCase("MATCH (a:person {age: $age})<-[:LINK]-(n) RETURN id(a) AS person")]
  public async Task PatternPropertiesBindBeforeBackendOperators(string query)
  {
    var rows = await Plan(query).ExecuteAsync(new Context(Parameters(("age", MixedValue.Int(30))))).ToListAsync();
    Assert.That(rows, Has.Count.EqualTo(1));
    Assert.That(rows[0].Values[0], Is.EqualTo(MixedValue.String("Bob")));
  }

  [Test]
  public async Task OptionalMatchFiltersAndFallbackUseBoundParameters()
  {
    var rows = await Plan("MATCH (a:person) OPTIONAL MATCH (a)-[:LINK]->(b {age: $age}) WHERE b.age > $minimum RETURN id(a) AS person, id(b) AS neighbor")
      .ExecuteAsync(new Context(Parameters(("age", MixedValue.Int(30)), ("minimum", MixedValue.Int(25))))).ToListAsync();
    Assert.That(rows, Has.Count.EqualTo(2));
    Assert.That(rows[0].Values[1], Is.EqualTo(MixedValue.String("Bob")));
    Assert.That(rows[1].Values[1]!.Value.IsNull(), Is.True);
  }

  [Test]
  public async Task ParameterizedLimitsWorkInReturnAndWith()
  {
    var plan = Plan("MATCH (n:person) WITH n ORDER BY n.age LIMIT $withLimit WHERE n.age >= $age RETURN id(n) AS person LIMIT $returnLimit");
    var rows = await plan.ExecuteAsync(new Context(Parameters(("withLimit", MixedValue.Int(2)),
      ("age", MixedValue.Int(30)), ("returnLimit", MixedValue.Int(1))))).ToListAsync();
    Assert.That(rows, Has.Count.EqualTo(1));
    Assert.That(rows[0].Values[0], Is.EqualTo(MixedValue.String("Bob")));
  }

  [TestCase("negative")]
  [TestCase("oversized")]
  [TestCase("double")]
  [TestCase("null")]
  [TestCase("string")]
  public void InvalidLimitsAreLocatedRuntimeErrors(string kind)
  {
    var value = kind switch
    {
      "negative" => MixedValue.Int(-1), "oversized" => MixedValue.Int((long)int.MaxValue + 1),
      "double" => MixedValue.Double(1), "string" => MixedValue.String("1"), _ => MixedValue.Null()
    };
    var error = Assert.ThrowsAsync<RuntimeException>(async () => await Plan("RETURN 1 LIMIT $limit")
      .ExecuteAsync(new Context(Parameters(("limit", value)))).ToListAsync());
    Assert.That(error!.Code, Is.EqualTo(value.Type == MixedValue.ValueType.Int
      ? CypherErrorCode.InvalidArgumentValue : CypherErrorCode.TypeMismatch));
    Assert.That(error.SourceSpan!.Start, Is.EqualTo(15));
  }

  [Test]
  public async Task LimitsCanBeZeroAndUnusedParametersAreIgnored()
  {
    var rows = await Plan("RETURN 1 LIMIT $limit").ExecuteAsync(new Context(Parameters(
      ("limit", MixedValue.Int(0)), ("unused", MixedValue.String("ignored"))))).ToListAsync();
    Assert.That(rows, Is.Empty);
  }

  private sealed class SpyFactory(InMemoryGraph graph) : TestOperatorFactory(graph)
  {
    public int Scans { get; private set; }
    protected override IOperator BuildNodeScan(VariableDefinition variable, string? label,
      PropertyFilter? propertyFilter, string? targetRelationshipLabel, RowSchema matchColumns)
    {
      Scans++;
      return base.BuildNodeScan(variable, label, propertyFilter, targetRelationshipLabel, matchColumns);
    }
  }

  [TestCase("MATCH (n:missing) RETURN $missing AS value")]
  [TestCase("MATCH (n:person) RETURN coalesce(1, $missing) AS value")]
  [TestCase("MATCH (n:person) RETURN 1 AS value LIMIT 0 UNION ALL RETURN $missing AS value")]
  [TestCase("MATCH (n:person) OPTIONAL MATCH (n)-[:LINK]->(b {age: $missing}) RETURN n")]
  public void MissingParametersFailBeforeAnyBackendBindingOrRows(string query)
  {
    ICypherParser parser = new CypherAstParser();
    var factory = new SpyFactory(Graph());
    var plan = parser.ParseQuery(query).BuildQueryPlan().BuildExecutionPlan(factory);
    var error = Assert.ThrowsAsync<RuntimeException>(async () => await plan.ExecuteAsync(new TestQueryContext()).ToListAsync());
    Assert.That(error!.Code, Is.EqualTo(CypherErrorCode.MissingParameter));
    Assert.That(error.SourceSpan!.Start, Is.EqualTo(query.IndexOf("$missing", StringComparison.Ordinal)));
    Assert.That(factory.Scans, Is.Zero);
  }

  [Test]
  public async Task SuppliedNullIsDifferentFromMissingAndNamesAreCaseSensitive()
  {
    var plan = Plan("RETURN $value IS NULL AS result");
    var rows = await plan.ExecuteAsync(new Context(Parameters(("value", MixedValue.Null())))).ToListAsync();
    Assert.That(rows[0].Values[0], Is.EqualTo(MixedValue.Bool(true)));
    var error = Assert.ThrowsAsync<RuntimeException>(async () => await plan.ExecuteAsync(new Context(
      Parameters(("Value", MixedValue.Null())))).ToListAsync());
    Assert.That(error!.Code, Is.EqualTo(CypherErrorCode.MissingParameter));
  }

  [Test]
  public async Task BindingSnapshotsMutableHostDictionariesForTheEntireExecution()
  {
    var values = new Dictionary<string, MixedValue> { ["value"] = MixedValue.Int(1) };
    var plan = Plan("RETURN $value AS value UNION ALL RETURN $value AS value");
    await using var rows = plan.ExecuteAsync(new Context(values)).GetAsyncEnumerator();
    Assert.That(await rows.MoveNextAsync(), Is.True);
    values["value"] = MixedValue.Int(2);
    Assert.That(await rows.MoveNextAsync(), Is.True);
    Assert.That(rows.Current.Values[0], Is.EqualTo(MixedValue.Int(1)));
    var next = await plan.ExecuteAsync(new Context(values)).ToListAsync();
    Assert.That(next[0].Values[0], Is.EqualTo(MixedValue.Int(2)));
  }

  [Test]
  public async Task ReusedPlansHaveIsolatedConcurrentParameterAndAggregationState()
  {
    var plan = Plan("MATCH (n:person) RETURN sum($amount) AS total, count(*) AS count");
    var results = await Task.WhenAll(Enumerable.Range(1, 20).Select(async amount =>
    {
      var rows = await plan.ExecuteAsync(new Context(Parameters(("amount", MixedValue.Int(amount))))).ToListAsync();
      Assert.That(rows[0].Values[0], Is.EqualTo(MixedValue.Int(amount * 2)));
      Assert.That(rows[0].Values[1], Is.EqualTo(MixedValue.Int(2)));
      return rows[0];
    }));
    Assert.That(results.All(r => ReferenceEquals(r.Schema, results[0].Schema)), Is.True);
  }

  [Test]
  public void ParsedQueriesExposeRequiredNamesAcrossUnionAndPreservePlaceholders()
  {
    const string query = "RETURN $x AS value LIMIT $limit UNION ALL RETURN $x + $y AS value";
    ICypherParser parser = new CypherAstParser();
    var ast = parser.ParseQuery(query);
    Assert.That(ast.RequiredParameters, Is.EqualTo(new[] { "x", "limit", "y" }));
    Assert.That(ast.PrettyPrint(), Is.EqualTo(query));
  }

  [TestCase("RETURN $0")]
  [TestCase("MATCH (n:$label) RETURN n")]
  [TestCase("MATCH (n)-[:$type]->(m) RETURN n")]
  [TestCase("RETURN 1 AS $alias")]
  [TestCase("CALL $procedure() YIELD value RETURN value")]
  public void ParametersCannotReplaceQueryStructure(string query)
  {
    ICypherParser parser = new CypherAstParser();
    Assert.Throws<ParseException>(() => parser.ParseQuery(query));
  }

  private sealed class ConfigPlugin : ICypherProcedurePlugin, ICypherProcedure
  {
    public string Namespace => "parameters";
    public string Name => "config";
    public IReadOnlyList<ProcedureField> Parameters => [new("options", MixedValue.ValueType.Map)];
    public IReadOnlyList<ProcedureField> Outputs => [new("value", MixedValue.ValueType.Int)];
    public IEnumerable<ICypherProcedure> GetProcedures() => [this];
    public async IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context, IRow inputRow,
      IReadOnlyList<MixedValue> arguments, RowSchema outputColumns,
      [EnumeratorCancellation] CancellationToken ct)
    {
      Assert.That(context, Is.TypeOf<Context>());
      await Task.Yield();
      ct.ThrowIfCancellationRequested();
      var row = context.RowFactory.EmptyRow(inputRow, outputColumns);
      row.Values[0] = arguments[0].AsMap()["limit"];
      yield return row;
    }
  }

  [Test]
  public async Task ProcedureConfigurationUsesMapParametersWithoutWrappingHostContext()
  {
    ICypherParser parser = new CypherAstParser();
    parser.InstallPlugin(new ConfigPlugin());
    var plan = parser.ParseQuery("CALL parameters.config($config) YIELD value RETURN value")
      .BuildQueryPlan().BuildExecutionPlan(new TestOperatorFactory(Graph()));
    var config = MixedValue.Map(new Dictionary<string, MixedValue> { ["limit"] = MixedValue.Int(12) });
    var rows = await plan.ExecuteAsync(new Context(Parameters(("config", config)))).ToListAsync();
    Assert.That(rows[0].Values[0], Is.EqualTo(MixedValue.Int(12)));
  }
}
