using System.Runtime.CompilerServices;
using Cyphoid.Core;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;
using Cyphoid.Core.Exceptions;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Tests.TestBackend;

namespace Cyphoid.Tests;

[TestFixture]
internal class SkipTests : TestHelper
{
  [SetUp]
  public void Setup()
  {
    Graph = new InMemoryGraph();
    foreach (var age in new[] { 10, 20, 30 })
    {
      Graph.AddNode(age.ToString(), "person");
      Graph.SetNodeProperty(age.ToString(), "age", (long)age);
    }
  }

  [TestCase(0, 3)]
  [TestCase(1, 2)]
  [TestCase(3, 0)]
  [TestCase(99, 0)]
  public async Task SkipAlone(int skip, int count) =>
    Assert.That(await BasicExecuteQuery($"MATCH (n) RETURN n SKIP {skip}"), Has.Count.EqualTo(count));

  [TestCase("MATCH (n) RETURN n.age AS age ORDER BY age SKIP 1 LIMIT 1")]
  [TestCase("MATCH (n) WITH n ORDER BY n.age SKIP 1 LIMIT 1 WHERE n.age = 20 RETURN n.age AS age")]
  [TestCase("MATCH (n) RETURN DISTINCT n.age AS age ORDER BY age SKIP 1 LIMIT 1")]
  public async Task ModifierOrderAndPrettyPrint(string query)
  {
    ICypherParser parser = new CypherAstParser();
    Assert.That(parser.ParseQuery(query).PrettyPrint(), Is.EqualTo(query));
    Assert.That((await BasicExecuteQuery(query)).Single().Values[0], Is.EqualTo(MixedValue.Int(20)));
  }

  [Test]
  public async Task AggregationUnionAndExists()
  {
    Assert.That(await BasicExecuteQuery("MATCH (n) RETURN count(*) AS total SKIP 1"), Is.Empty);
    Assert.That(await BasicExecuteQuery("RETURN 1 AS value SKIP 1 UNION ALL RETURN 2 AS value SKIP 0"), Has.Count.EqualTo(1));
    var rows = await BasicExecuteQuery("RETURN EXISTS { RETURN 1 SKIP 1 } AS found");
    Assert.That(rows.Single().Values[0], Is.EqualTo(MixedValue.Bool(false)));
  }

  private sealed class Context(MixedValue offset) : IQueryContext
  {
    public IQueryRowFactory RowFactory => ((IQueryContext)new TestQueryContext()).RowFactory;
    public IReadOnlyDictionary<string, MixedValue> Parameters => new Dictionary<string, MixedValue> { ["offset"] = offset, ["count"] = MixedValue.Int(1) };
  }

  [Test]
  public async Task ParametersRebindAndKeepSourceOrder()
  {
    ICypherParser parser = new CypherAstParser();
    var query = parser.ParseQuery("MATCH (n) RETURN n.age AS age ORDER BY age SKIP $offset LIMIT $count");
    Assert.That(query.RequiredParameters, Is.EqualTo(new[] { "offset", "count" }));
    var plan = query.BuildQueryPlan().BuildExecutionPlan(new TestOperatorFactory(Graph));
    foreach (var offset in new[] { 0, 1, 2 })
    {
      var rows = await plan.ExecuteAsync(new Context(MixedValue.Int(offset))).ToListAsync();
      Assert.That(rows.Single().Values[0], Is.EqualTo(MixedValue.Int((offset + 1) * 10)));
    }
  }

  [TestCase(-1L)]
  [TestCase(2147483648L)]
  public void InvalidParameterRanges(long offset) => Assert.ThrowsAsync<RuntimeException>(async () =>
    await Plan().ExecuteAsync(new Context(MixedValue.Int(offset))).ToListAsync());

  private IOperator Plan() => ((ICypherParser)new CypherAstParser()).ParseQuery("RETURN 1 SKIP $offset")
    .BuildQueryPlan().BuildExecutionPlan(new TestOperatorFactory(Graph));

  [Test]
  public void InvalidTypesAndMissingParameters()
  {
    foreach (var value in new[] { MixedValue.Null(), MixedValue.String("1"), MixedValue.Double(1) })
    {
      var error = Assert.ThrowsAsync<RuntimeException>(async () => await Plan().ExecuteAsync(new Context(value)).ToListAsync());
      Assert.That(error!.Code, Is.EqualTo(CypherErrorCode.TypeMismatch));
      Assert.That(error.SourceSpan, Is.Not.Null);
    }
    Assert.ThrowsAsync<RuntimeException>(async () => await Plan().ExecuteAsync(new TestQueryContext()).ToListAsync());
  }

  [TestCase("RETURN 1 SKIP -1")]
  [TestCase("RETURN 1 SKIP 2147483648")]
  [TestCase("RETURN 1 SKIP 1.5")]
  [TestCase("RETURN 1 LIMIT 1 SKIP 1")]
  public void InvalidSyntaxOrLiteral(string query) =>
    Assert.Throws<ParseException>(() => ((ICypherParser)new CypherAstParser()).ParseQuery(query));

  private sealed class Source : IOperator
  {
    public bool Disposed;
    public IRow Row = new Row(RowSchema.Empty);
    public async IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context, [EnumeratorCancellation] CancellationToken ct = default)
    {
      try { while (true) { ct.ThrowIfCancellationRequested(); await Task.Yield(); yield return Row; } }
      finally { Disposed = true; }
    }
  }

  [Test]
  public async Task PreservesRowIdentityAndDisposesOnEarlyExit()
  {
    var source = new Source();
    await foreach (var row in new SkipReferenceOperator(source, 2).ExecuteAsync(new TestQueryContext()))
    {
      Assert.That(row, Is.SameAs(source.Row));
      break;
    }
    Assert.That(source.Disposed, Is.True);
  }

  [Test]
  public async Task CancellationWhileDiscardingDisposesSource()
  {
    var source = new Source();
    using var cts = new CancellationTokenSource();
    cts.CancelAfter(30);
    Assert.ThrowsAsync<OperationCanceledException>(async () =>
    {
      await foreach (var row in new SkipReferenceOperator(source, int.MaxValue)
        .ExecuteAsync(new TestQueryContext()).WithCancellation(cts.Token)) { }
    });
    Assert.That(source.Disposed, Is.True);
    await Task.CompletedTask;
  }
}
