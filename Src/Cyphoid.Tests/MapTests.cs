using System.Runtime.CompilerServices;
using Cyphoid.Core;
using Cyphoid.Core.Exceptions;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;
using Cyphoid.Core.Procedures;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Tests.TestBackend;

namespace Cyphoid.Tests;

[TestFixture]
internal class MapTests : TestHelper
{
  [Test]
  public void MixedValueMapsAreImmutableSnapshotsWithOrdinalKeys()
  {
    var input = new Dictionary<string, MixedValue>(StringComparer.OrdinalIgnoreCase) { ["Name"] = MixedValue.String("Alice") };
    var value = MixedValue.Map(input);
    input["Name"] = MixedValue.String("Bob");
    Assert.That(value.Type, Is.EqualTo(MixedValue.ValueType.Map));
    Assert.That(value.TryGetMap(out var map), Is.True);
    Assert.That(map!["Name"], Is.EqualTo(MixedValue.String("Alice")));
    Assert.That(map.ContainsKey("name"), Is.False);
    Assert.Throws<NotSupportedException>(() => ((IDictionary<string, MixedValue>)map).Add("new", MixedValue.Int(1)));
    Assert.That(MixedValue.Map(null).IsNull(), Is.True);
  }

  [Test]
  public void NestedMapsRoundTripThroughHostObjects()
  {
    var host = new Dictionary<string, object?>
    {
      ["config"] = new Dictionary<string, object?> { ["limit"] = 10L, ["missing"] = null }
    };
    var value = MixedValue.FromObject(host);
    host.Clear();
    Assert.That(value.AsMap()["config"].AsMap()["limit"], Is.EqualTo(MixedValue.Int(10)));
    Assert.That(MixedValue.FromObject(value.AsObject()), Is.EqualTo(value));
    Assert.That(MixedValue.FromObject(value), Is.EqualTo(value));
    Assert.That(value.Match(() => 0, _ => 0, _ => 0, _ => 0, _ => 0, _ => 0, m => m.Count), Is.EqualTo(1));
    Assert.That(value.ToString(), Does.Contain("config").And.Contain("limit"));
    Assert.Throws<RuntimeException>(() => MixedValue.Int(1).AsMap());
  }

  [Test]
  public void EqualityAndHashingAreStructuralAndIndependentOfKeyOrder()
  {
    var left = MixedValue.Map(new Dictionary<string, MixedValue>
      { ["a"] = MixedValue.Int(1), ["nested"] = MixedValue.Map(new Dictionary<string, MixedValue> { ["x"] = MixedValue.Null() }) });
    var right = MixedValue.Map(new Dictionary<string, MixedValue>
      { ["nested"] = MixedValue.Map(new Dictionary<string, MixedValue> { ["x"] = MixedValue.Null() }), ["a"] = MixedValue.Int(1) });
    Assert.That(left.Equals(right), Is.True);
    Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
    Assert.That(new HashSet<MixedValue> { left, right }, Has.Count.EqualTo(1));
    Assert.That(left.Equals(MixedValue.Map(new Dictionary<string, MixedValue> { ["a"] = MixedValue.Int(2) })), Is.False);
  }

  [Test]
  public void NestedNodesWithEqualIdentityHashEqually()
  {
    var left = MixedValue.Map(new Dictionary<string, MixedValue>
      { ["node"] = MixedValue.GraphNode(new GraphNode<string>("same", "person", [], [])) });
    var right = MixedValue.Map(new Dictionary<string, MixedValue>
      { ["node"] = MixedValue.GraphNode(new GraphNode<string>("same", "person", [], [])) });
    Assert.That(left.Equals(right), Is.True);
    Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
  }

  [TestCase("RETURN {} AS value")]
  [TestCase("RETURN {name: \"Alice\", age: 42, enabled: true, missing: null} AS value")]
  [TestCase("WITH 3 AS n RETURN {nested: {value: n + 1}, limit: n} AS value")]
  public async Task ConstructsAndPrettyPrintsMaps(string query)
  {
    ICypherParser parser = new CypherAstParser();
    Assert.That(parser.ParseQuery(query).PrettyPrint(), Is.EqualTo(query));
    var rows = await BasicExecuteQuery(query);
    Assert.That(rows, Has.Count.EqualTo(1));
    Assert.That(rows[0].Values[0]!.Value.Type, Is.EqualTo(MixedValue.ValueType.Map));
  }

  [TestCase("RETURN {a: 1}.a AS value", 1L)]
  [TestCase("RETURN {a: {b: 2}}.a.b AS value", 2L)]
  [TestCase("WITH {a: {b: 2}} AS m RETURN m.a.b AS value", 2L)]
  [TestCase("WITH {a: {b: 2}} AS m RETURN m['a']['b'] AS value", 2L)]
  [TestCase("WITH {a: {b: 2}} AS m RETURN m['a'].b AS value", 2L)]
  [TestCase("WITH {limit: 5} AS m RETURN m.limit AS value", 5L)]
  [TestCase("WITH {a: 7} AS m, 'a' AS key RETURN m[key] AS value", 7L)]
  [TestCase("RETURN {a: 1}.missing AS value", null)]
  [TestCase("RETURN {a: 1}.A AS value", null)]
  [TestCase("RETURN {a: null}.a.b AS value", null)]
  [TestCase("RETURN {a: 1}[null] AS value", null)]
  [TestCase("RETURN (null)['a'] AS value", null)]
  [TestCase("WITH null AS m RETURN m.a AS value", null)]
  public async Task LooksUpStaticAndDynamicKeys(string query, object? expected)
  {
    var result = await ExecuteQuery(query);
    Assert.That(result.Rows[0]["value"], Is.EqualTo(expected));
  }

  [Test]
  public async Task MapsSupportFilteringGroupingDistinctAndUnion()
  {
    Graph.AddNode("a", "person");
    Graph.AddNode("b", "person");
    var grouped = await ExecuteQuery("MATCH (n:person) WITH {x: 1} AS m WHERE m.x = 1 RETURN m, count(*) AS total");
    Assert.That(grouped.Rows, Has.Count.EqualTo(1));
    Assert.That(grouped.Rows[0]["total"], Is.EqualTo(2L));
    var distinct = await ExecuteQuery("MATCH (n:person) RETURN DISTINCT {x: 1} AS m");
    Assert.That(distinct.Rows, Has.Count.EqualTo(1));
    var union = await ExecuteQuery("RETURN {x: 1, y: {z: null}} AS m UNION RETURN {y: {z: null}, x: 1} AS m");
    Assert.That(union.Rows, Has.Count.EqualTo(1));
    var equality = await ExecuteQuery("RETURN {a: 1, b: 2} = {b: 2, a: 1} AS value");
    Assert.That(equality.Rows[0]["value"], Is.EqualTo(true));
  }

  [TestCase("{a: null} = {a: null}", null)]
  [TestCase("{a: null} <> {a: null}", null)]
  [TestCase("{a: null, b: 1} = {a: null, b: 2}", false)]
  [TestCase("{a: null} = {b: null}", false)]
  [TestCase("{a: {b: null}} = {a: {b: null}}", null)]
  [TestCase("{a: 1} = null", null)]
  [TestCase("{a: null} IN [{a: null}]", null)]
  [TestCase("{a: 1} IN [{a: null}, {a: 1}]", true)]
  public async Task MapQueryEqualityPropagatesUnknownValues(string expression, object? expected)
  {
    var result = await ExecuteQuery("RETURN " + expression + " AS value");
    Assert.That(result.Rows[0]["value"], Is.EqualTo(expected));
  }

  [Test]
  public async Task ProjectsNodePropertiesAndRetainsExplicitOverrides()
  {
    Graph.AddNode("a", "person");
    Graph.SetNodeProperty("a", "name", "Alice");
    Graph.SetNodeProperty("a", "age", 40L);
    var result = await ExecuteQuery("MATCH (n:person) RETURN n {.*, .missing, age: n.age + 1, who: n.name} AS value");
    var map = (IReadOnlyDictionary<string, object?>)result.Rows[0]["value"]!;
    Assert.That(map["name"], Is.EqualTo("Alice"));
    Assert.That(map["age"], Is.EqualTo(41L));
    Assert.That(map["missing"], Is.Null);
    Assert.That(map["who"], Is.EqualTo("Alice"));
  }

  [Test]
  public async Task NodeDynamicAccessAndNestedHostMapsWork()
  {
    Graph.AddNode("a", "person");
    Graph.SetNodeProperty("a", "config", new Dictionary<string, object?> { ["limit"] = 5L });
    var result = await ExecuteQuery("MATCH (n:person) RETURN n['config'].limit AS value");
    Assert.That(result.Rows[0]["value"], Is.EqualTo(5L));
  }

  [TestCase("WITH {a: 1, b: 2} AS m RETURN m {.a, sum: m.a + m.b} AS value")]
  [TestCase("WITH {a: 1} AS m, 5 AS maxRows RETURN m {maxRows, .a} AS value")]
  [TestCase("WITH {a: 1} AS m, 5 AS value RETURN m {value: value} AS result")]
  [TestCase("WITH null AS m RETURN m {.a} AS value")]
  public async Task MapProjectionsRoundTrip(string query)
  {
    ICypherParser parser = new CypherAstParser();
    Assert.That(parser.ParseQuery(query).PrettyPrint(), Is.EqualTo(query));
    var result = await ExecuteQuery(query);
    Assert.That(result.Rows, Has.Count.EqualTo(1));
  }

  [TestCase("RETURN {a: 1, a: 2}", CypherErrorCode.InvalidArgumentValue)]
  [TestCase("RETURN {a: count(*)}", CypherErrorCode.InvalidAggregateUsage)]
  [TestCase("RETURN {a: 1}.a + count(*)", CypherErrorCode.InvalidAggregateUsage)]
  public void InvalidMapsFailDuringParsing(string query, CypherErrorCode code)
  {
    ICypherParser parser = new CypherAstParser();
    var error = Assert.Throws<ParseException>(() => parser.ParseQuery(query));
    Assert.That(error!.Code, Is.EqualTo(code));
  }

  [TestCase("RETURN {a: 1}[0]")]
  [TestCase("RETURN (1).a")]
  [TestCase("WITH 1 AS m RETURN m {.a}")]
  [TestCase("RETURN toString({a: 1})")]
  [TestCase("RETURN {} < {}")]
  public void InvalidMapOperationsAreLocatedRuntimeErrors(string query)
  {
    var error = Assert.ThrowsAsync<RuntimeException>(async () => await ExecuteQuery(query));
    Assert.That(error!.Code, Is.EqualTo(CypherErrorCode.TypeMismatch));
    Assert.That(error.SourceSpan, Is.Not.Null);
  }

  private sealed class ConfigPlugin : ICypherProcedurePlugin, ICypherProcedure
  {
    public string Namespace => "config";
    public string Name => "run";
    public IReadOnlyList<ProcedureField> Parameters => [new("options", MixedValue.ValueType.Map)];
    public IReadOnlyList<ProcedureField> Outputs => [new("settings", MixedValue.ValueType.Map), new("configuredLimit", MixedValue.ValueType.Int)];
    public IEnumerable<ICypherProcedure> GetProcedures() => [this];
    public async IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context, IRow inputRow,
      IReadOnlyList<MixedValue> arguments, RowSchema outputColumns,
      [EnumeratorCancellation] CancellationToken ct)
    {
      await Task.Yield();
      ct.ThrowIfCancellationRequested();
      var config = arguments[0].AsMap();
      var row = context.RowFactory.EmptyRow(inputRow, outputColumns);
      ((TestRow)row).NodeIdsForRow.Add("configured");
      row.Values[0] = arguments[0];
      row.Values[1] = config["limit"];
      yield return row;
    }
  }

  [TestCase("CALL config.run({limit: 10, nested: {enabled: true}}) YIELD settings, configuredLimit RETURN settings.nested.enabled AS enabled, configuredLimit")]
  [TestCase("WITH {limit: 10, nested: {enabled: true}} AS options CALL config.run(options) YIELD settings, configuredLimit RETURN settings.nested.enabled AS enabled, configuredLimit")]
  [TestCase("WITH {limit: 9} AS options CALL config.run(options {.*, limit: options.limit + 1, nested: {enabled: true}}) YIELD settings, configuredLimit RETURN settings.nested.enabled AS enabled, configuredLimit")]
  public async Task ProceduresAcceptMapConfigurationAndYieldMaps(string query)
  {
    ICypherParser parser = new CypherAstParser();
    parser.InstallPlugin(new ConfigPlugin());
    var rows = await parser.ParseQuery(query).BuildQueryPlan().BuildExecutionPlan(new TestOperatorFactory(Graph))
      .ExecuteAsync(new TestQueryContext()).ToListAsync();
    Assert.That(rows, Has.Count.EqualTo(1));
    Assert.That(rows[0].Values[0], Is.EqualTo(MixedValue.Bool(true)));
    Assert.That(rows[0].Values[1], Is.EqualTo(MixedValue.Int(10)));
    Assert.That(((TestRow)rows[0]).NodeIdsForRow, Does.Contain("configured"));
  }

  [TestCase("null")]
  [TestCase("1")]
  public void ProcedureMapSignatureRejectsOtherTypes(string argument)
  {
    ICypherParser parser = new CypherAstParser();
    parser.InstallPlugin(new ConfigPlugin());
    var error = Assert.ThrowsAsync<RuntimeException>(async () => await parser.ParseQuery(
      $"CALL config.run({argument}) YIELD settings RETURN settings").BuildQueryPlan()
      .BuildExecutionPlan(new TestOperatorFactory(Graph)).ExecuteAsync(new TestQueryContext()).ToListAsync());
    Assert.That(error!.Code, Is.EqualTo(CypherErrorCode.TypeMismatch));
  }
}
