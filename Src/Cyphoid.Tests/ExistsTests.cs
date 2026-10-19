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
internal class ExistsTests : TestHelper
{
  [SetUp]
  public void Setup()
  {
    Graph = new InMemoryGraph();
    Graph.AddNode("Alice", "person");
    Graph.AddNode("Bob", "person");
    Graph.AddNode("Cat", "animal");
    Graph.SetNodeProperty("Alice", "age", 20L);
    Graph.SetNodeProperty("Bob", "age", 30L);
    Graph.AddEdge("Alice", "Bob", "KNOWS");
    Graph.AddEdge("Alice", "Cat", "OWNS");
  }

  [TestCase("EXISTS { MATCH (n)-[:KNOWS]->(other) }", "Alice")]
  [TestCase("NOT EXISTS { MATCH (n)-[:KNOWS]->(other) }", "Bob")]
  [TestCase("EXISTS { (n)-[:KNOWS]->(other) WHERE other.age > n.age }", "Alice")]
  [TestCase("EXISTS { MATCH (other)-[:KNOWS]->(n) }", "Bob")]
  [TestCase("EXISTS { MATCH (n)-[:KNOWS]->(other) MATCH (n)-[:OWNS]->(pet) }", "Alice")]
  [TestCase("EXISTS { MATCH (n)-[:KNOWS]->(other), (pet:animal) }", "Alice")]
  [TestCase("EXISTS { MATCH (n)-[:KNOWS]->(other) RETURN other }", "Alice")]
  [TestCase("EXISTS { MATCH (n)-[:KNOWS]->(other) RETURN other LIMIT 1 }", "Alice")]
  [TestCase("EXISTS { MATCH (n)-[:KNOWS]->(other) WHERE NOT EXISTS { MATCH (other)-[:KNOWS]->(third) } }", "Alice")]
  [TestCase("EXISTS { MATCH (n)-[:OWNS]->(pet) UNION MATCH (n)-[:KNOWS]->(other) }", "Alice")]
  public async Task CorrelatedFilters(string predicate, string expected)
  {
    var result = await ExecuteQuery("MATCH (n:person) WHERE " + predicate + " RETURN id(n) AS person");
    Assert.That(result.Rows, Has.Count.EqualTo(1));
    Assert.That(result.Rows[0]["person"], Is.EqualTo(expected));
  }

  [TestCase("EXISTS { MATCH (n)-[:KNOWS]->(other) }")]
  [TestCase("coalesce(EXISTS { MATCH (n)-[:KNOWS]->(other) }, false)")]
  [TestCase("{found: EXISTS { MATCH (n)-[:KNOWS]->(other) }}.found")]
  [TestCase("{exists: EXISTS { MATCH (n)-[:KNOWS]->(other) }}.exists")]
  [TestCase("EXISTS { MATCH (n)-[:KNOWS]->(other) } IN [true]")]
  [TestCase("{a: 1} {found: EXISTS { MATCH (n)-[:KNOWS]->(other) }}.found")]
  public async Task ExistsWorksInProjectedExpressions(string expression)
  {
    var result = await ExecuteQuery("MATCH (n:person) RETURN id(n) AS person, " + expression + " AS found ORDER BY person");
    Assert.That(result.Rows, Has.Count.EqualTo(2));
    Assert.That(result.Rows[0]["found"], Is.EqualTo(true));
    Assert.That(result.Rows[1]["found"], Is.EqualTo(false));
  }

  [Test]
  public async Task MultipleExistsExpressionsUseSeparateMaterializedSlots()
  {
    var result = await ExecuteQuery("MATCH (n:person) RETURN EXISTS { MATCH (n)-[:KNOWS]->(other) } AS knows, EXISTS { MATCH (n)-[:OWNS]->(pet) } AS owns, id(n) AS person ORDER BY person");
    Assert.That(result.Rows[0]["knows"], Is.EqualTo(true));
    Assert.That(result.Rows[0]["owns"], Is.EqualTo(true));
    Assert.That(result.Rows[1]["knows"], Is.EqualTo(false));
    Assert.That(result.Rows[1]["owns"], Is.EqualTo(false));
  }

  [Test]
  public async Task ExistsInWithAndOrderingPreservesOuterSlotsAndSchemas()
  {
    var result = await ExecuteQuery("MATCH (n:person) WITH n, EXISTS { MATCH (n)-[:KNOWS]->(other) } AS found WHERE found RETURN id(n) AS person");
    Assert.That(result.Rows[0]["person"], Is.EqualTo("Alice"));
    var rows = await BasicExecuteQuery("MATCH (n:person) RETURN id(n) AS person ORDER BY EXISTS { MATCH (n)-[:KNOWS]->(other) } DESC");
    Assert.That(rows, Has.Count.EqualTo(2));
    Assert.That(rows[0].Values[0], Is.EqualTo(MixedValue.String("Alice")));
    Assert.That(rows.All(r => r.Columns.Count == 1), Is.True);
    Assert.That(rows[0].Schema, Is.SameAs(rows[1].Schema));
  }

  [Test]
  public async Task ExistsDoesNotMultiplyOuterRowsOrLeakMetadata()
  {
    Graph.AddEdge("Alice", "Cat", "KNOWS");
    var rows = await BasicExecuteQuery("MATCH (n:person) WHERE EXISTS { MATCH (n)-[:KNOWS]->(other) } RETURN n");
    Assert.That(rows, Has.Count.EqualTo(1));
    Assert.That(((TestRow)rows[0]).NodeIdsForRow, Is.EquivalentTo(new[] { "Alice" }));
  }

  [TestCase("EXISTS { MATCH (m:missing) }", false)]
  [TestCase("EXISTS { MATCH (m:animal) }", true)]
  [TestCase("EXISTS { RETURN 1 }", true)]
  [TestCase("EXISTS { RETURN null }", true)]
  [TestCase("EXISTS { RETURN 1 LIMIT 0 }", false)]
  [TestCase("EXISTS { MATCH (m:missing) RETURN count(*) AS total }", true)]
  [TestCase("EXISTS { OPTIONAL MATCH (m:missing) }", true)]
  [TestCase("EXISTS { MATCH (m:missing) UNION ALL MATCH (m:animal) }", true)]
  [TestCase("EXISTS { MATCH (m:missing) RETURN m UNION ALL RETURN 1 AS value }", true)]
  public async Task ExistenceDependsOnRowsRatherThanValues(string expression, bool expected)
  {
    var result = await ExecuteQuery("RETURN " + expression + " AS found");
    Assert.That(result.Rows[0]["found"], Is.EqualTo(expected));
  }

  [Test]
  public async Task NullCorrelatedNodesHaveNoMatches()
  {
    var result = await ExecuteQuery("WITH null AS n RETURN EXISTS { MATCH (n)-[:KNOWS]->(other) } AS found");
    Assert.That(result.Rows[0]["found"], Is.EqualTo(false));
  }

  [Test]
  public async Task GroupingAndAggregateArgumentsCanContainExists()
  {
    var result = await ExecuteQuery("MATCH (n:person) RETURN EXISTS { MATCH (n)-[:KNOWS]->(other) } AS found, count(*) AS total");
    Assert.That(result.Rows, Has.Count.EqualTo(2));
    Assert.That(result.Rows.All(r => Equals(r["total"], 1L)), Is.True);
    var count = await ExecuteQuery("MATCH (n:person) RETURN count(EXISTS { MATCH (n)-[:KNOWS]->(other) }) AS total");
    Assert.That(count.Rows[0]["total"], Is.EqualTo(2L));
  }

  [TestCase("false AND EXISTS { RETURN 1 / 0 }")]
  [TestCase("NOT (true OR EXISTS { RETURN 1 / 0 })")]
  [TestCase("coalesce(false, EXISTS { RETURN 1 / 0 })")]
  [TestCase("false IN [false, EXISTS { RETURN 1 / 0 }]")]
  public async Task LazyExpressionsSkipUnneededSubqueries(string expression)
  {
    var result = await ExecuteQuery("RETURN " + expression + " AS value");
    Assert.That(result.Rows, Has.Count.EqualTo(1));
  }

  [Test]
  public void SubqueryLocalsAreNotVisibleOutside()
  {
    ICypherParser parser = new CypherAstParser();
    var error = Assert.Throws<ParseException>(() => parser.ParseQuery("MATCH (n) WHERE EXISTS { MATCH (n)-->(local) } RETURN local"));
    Assert.That(error!.Code, Is.EqualTo(CypherErrorCode.UnknownVariable));
  }

  [Test]
  public async Task LocalNamesCanBeReusedByIndependentSubqueriesAndOuterMatches()
  {
    var result = await ExecuteQuery("MATCH (n:person) WHERE EXISTS { MATCH (local:animal) } AND EXISTS { MATCH (local:person) } MATCH (local:animal) RETURN id(local) AS animal");
    Assert.That(result.Rows, Has.Count.EqualTo(2));
    Assert.That(result.Rows.All(r => Equals(r["animal"], "Cat")), Is.True);
  }

  [Test]
  public void ParametersInsideSubqueriesAreRequiredBeforeOuterBackendAccess()
  {
    ICypherParser parser = new CypherAstParser();
    var query = parser.ParseQuery("RETURN false AND EXISTS { MATCH (n {age: $age}) } AS found");
    Assert.That(query.RequiredParameters, Is.EqualTo(new[] { "age" }));
    var error = Assert.ThrowsAsync<RuntimeException>(async () => await query.BuildQueryPlan()
      .BuildExecutionPlan(new TestOperatorFactory(Graph)).ExecuteAsync(new TestQueryContext()).ToListAsync());
    Assert.That(error!.Code, Is.EqualTo(CypherErrorCode.MissingParameter));
  }

  [Test]
  public void ErrorsInsideExistsKeepTheirLocation()
  {
    const string query = "RETURN EXISTS { RETURN 1 / 0 } AS found";
    var error = Assert.ThrowsAsync<RuntimeException>(async () => await ExecuteQuery(query));
    Assert.That(error!.Code, Is.EqualTo(CypherErrorCode.DivisionByZero));
    Assert.That(error.SourceSpan!.Start, Is.EqualTo(query.IndexOf("1 / 0", StringComparison.Ordinal)));
  }

  [TestCase("WITH 25 AS minimum MATCH (n)-[:KNOWS]->(other) WHERE other.age > minimum", "Alice")]
  [TestCase("WITH n MATCH (n)-[:KNOWS]->(other)", "Alice")]
  [TestCase("WITH 1 AS x WITH x + 1 AS y MATCH (n)-[:KNOWS]->(other) WHERE y = 2", "Alice")]
  public async Task WithPreservesOuterVariables(string body, string expected)
  {
    var result = await ExecuteQuery("MATCH (n:person) WHERE EXISTS { " + body + " } RETURN id(n) AS person");
    Assert.That(result.Rows, Has.Count.EqualTo(1));
    Assert.That(result.Rows[0]["person"], Is.EqualTo(expected));
  }

  [Test]
  public async Task ImportedVariablesDoNotPreventEmptyInputGlobalAggregation()
  {
    var result = await ExecuteQuery("MATCH (n:person) WHERE EXISTS { MATCH (other:missing) WITH count(*) AS total WHERE total = 0 RETURN n } RETURN id(n) AS person");
    Assert.That(result.Rows, Has.Count.EqualTo(2));
  }

  [Test]
  public void WithCannotShadowImportedVariables()
  {
    ICypherParser parser = new CypherAstParser();
    var error = Assert.Throws<ParseException>(() => parser.ParseQuery("MATCH (n) WHERE EXISTS { WITH 1 AS n RETURN n } RETURN n"));
    Assert.That(error!.Message, Does.Contain("shadow"));
  }

  [Test]
  public void PrettyPrintingHidesImplicitImportedProjections()
  {
    const string query = "MATCH (n) WHERE EXISTS { WITH 1 AS x MATCH (n)-->(other) WHERE x = 1 RETURN other } RETURN n";
    ICypherParser parser = new CypherAstParser();
    Assert.That(parser.ParseQuery(query).PrettyPrint(), Is.EqualTo(query.Replace("-->", "-[]->")));
  }

  private sealed class ParameterContext(QueryParameters parameters) : IQueryContext
  {
    private readonly IQueryContext inner = new TestQueryContext();
    public IQueryRowFactory RowFactory => inner.RowFactory;
    public IReadOnlyDictionary<string, MixedValue> Parameters => parameters;
  }

  [Test]
  public async Task ConcurrentExecutionsBindSubqueryParametersIndependently()
  {
    ICypherParser parser = new CypherAstParser();
    var plan = parser.ParseQuery("MATCH (n:person) RETURN EXISTS { MATCH (n)-[:KNOWS]->(other {age: $age}) } AS found ORDER BY found DESC")
      .BuildQueryPlan().BuildExecutionPlan(new TestOperatorFactory(Graph));
    await Task.WhenAll(Enumerable.Range(0, 10).Select(async index =>
    {
      var age = index % 2 == 0 ? 30 : 99;
      var rows = await plan.ExecuteAsync(new ParameterContext(new QueryParameters(new Dictionary<string, MixedValue>
        { ["age"] = MixedValue.Int(age) }))).ToListAsync();
      Assert.That(rows[0].Values[0], Is.EqualTo(MixedValue.Bool(age == 30)));
      Assert.That(rows[1].Values[0], Is.EqualTo(MixedValue.Bool(false)));
    }));
  }

  private sealed class ProbeFactory(InMemoryGraph graph) : TestOperatorFactory(graph)
  {
    public bool Wait { get; init; }
    public bool Disposed { get; private set; }
    public int Reads { get; private set; }
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected override IOperator BuildNodeScan(VariableDefinition variable, string? label,
      PropertyFilter? propertyFilter, string? targetRelationshipType, RowSchema matchColumns) => label == "probe"
      ? new Probe(this, variable, matchColumns) : base.BuildNodeScan(variable, label, propertyFilter, targetRelationshipType, matchColumns);
    private sealed record Probe(ProbeFactory Owner, VariableDefinition Variable, RowSchema Schema) : IOperator
    {
      public async IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context, [EnumeratorCancellation] CancellationToken ct = default)
      {
        try
        {
          Owner.Reads++;
          if (Owner.Wait)
          {
            Owner.Started.TrySetResult();
            await Task.Delay(Timeout.Infinite, ct);
          }
          ct.ThrowIfCancellationRequested();
          var row = context.RowFactory.EmptyRow(Schema);
          row.Values[Variable.SlotIndex] = MixedValue.GraphNode(new GraphNode<string>("probe", "probe", [], []));
          yield return row;
          throw new InvalidOperationException("EXISTS read past its first match.");
        }
        finally { Owner.Disposed = true; }
      }
    }
  }

  [Test]
  public async Task ExistsStopsAtFirstRowAndDisposesBackendEnumerator()
  {
    ICypherParser parser = new CypherAstParser();
    var factory = new ProbeFactory(Graph);
    var rows = await parser.ParseQuery("RETURN EXISTS { MATCH (n:probe) } AS found").BuildQueryPlan()
      .BuildExecutionPlan(factory).ExecuteAsync(new TestQueryContext()).ToListAsync();
    Assert.That(rows[0].Values[0], Is.EqualTo(MixedValue.Bool(true)));
    Assert.That(factory.Reads, Is.EqualTo(1));
    Assert.That(factory.Disposed, Is.True);
  }

  [TestCase(false)]
  [TestCase(true)]
  public async Task CancellationReachesPendingSubqueryIoAndDisposesIt(bool enumerationToken)
  {
    ICypherParser parser = new CypherAstParser();
    var factory = new ProbeFactory(Graph) { Wait = true };
    var plan = parser.ParseQuery("RETURN EXISTS { MATCH (n:probe) } AS found").BuildQueryPlan().BuildExecutionPlan(factory);
    using var cancellation = new CancellationTokenSource();
    var pending = Consume(plan.ExecuteAsync(new TestQueryContext(), enumerationToken ? default : cancellation.Token),
      enumerationToken ? cancellation.Token : default);
    await factory.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
    cancellation.Cancel();
    Assert.CatchAsync<OperationCanceledException>(async () => await pending.WaitAsync(TimeSpan.FromSeconds(5)));
    Assert.That(factory.Disposed, Is.True);
  }

  private static async Task Consume(IAsyncEnumerable<IRow> rows, CancellationToken ct)
  {
    await foreach (var row in rows.WithCancellation(ct)) Assert.That(row, Is.Not.Null);
  }

  private sealed class EchoPlugin : ICypherProcedurePlugin, ICypherProcedure
  {
    public string Namespace => "echo";
    public string Name => "flag";
    public IReadOnlyList<ProcedureField> Parameters => [new("flag", MixedValue.ValueType.Bool)];
    public IReadOnlyList<ProcedureField> Outputs => [new("flag", MixedValue.ValueType.Bool)];
    public IEnumerable<ICypherProcedure> GetProcedures() => [this];
    public async IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context, IRow inputRow, IReadOnlyList<MixedValue> arguments,
      RowSchema outputColumns, [EnumeratorCancellation] CancellationToken ct)
    {
      await Task.Yield();
      ct.ThrowIfCancellationRequested();
      var row = context.RowFactory.EmptyRow(inputRow, outputColumns);
      row.Values[0] = arguments[0];
      yield return row;
    }
  }

  [Test]
  public async Task ExistsCanCallProceduresAndBePassedAsAnArgument()
  {
    ICypherParser parser = new CypherAstParser();
    parser.InstallPlugin(new EchoPlugin());
    var rows = await parser.ParseQuery("MATCH (n:person) CALL echo.flag(EXISTS { CALL echo.flag(EXISTS { MATCH (n)-[:KNOWS]->(other) }) YIELD flag RETURN flag }) YIELD flag RETURN flag")
      .BuildQueryPlan().BuildExecutionPlan(new TestOperatorFactory(Graph)).ExecuteAsync(new TestQueryContext()).ToListAsync();
    // A procedure returns one row even when its flag is false: existence is true.
    Assert.That(rows, Has.Count.EqualTo(2));
    Assert.That(rows.All(r => r.Values[0]!.Value.AsBool()), Is.True);
  }
}
