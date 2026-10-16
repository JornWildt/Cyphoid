using System.Runtime.CompilerServices;
using Cyphoid.Core;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;
using Cyphoid.Core.Procedures;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Tests.TestBackend;

namespace Cyphoid.Tests;

[TestFixture]
public class CancellationTests
{
  private sealed class BlockingPlugin : ICypherProcedurePlugin, ICypherProcedure
  {
    public string Namespace => "blocking";
    public string Name => "rows";
    public IReadOnlyList<ProcedureField> Parameters => [];
    public IReadOnlyList<ProcedureField> Outputs =>
      [new("value", MixedValue.ValueType.Int), new("node", MixedValue.ValueType.Node)];
    public IEnumerable<ICypherProcedure> GetProcedures() => [this];
    public TaskCompletionSource Waiting { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public CancellationToken ReceivedToken { get; private set; }
    public int Disposals { get; private set; }
    public bool Block { get; init; } = true;

    public async IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context, IRow inputRow,
      IReadOnlyList<MixedValue> arguments, RowSchema outputColumns,
      [EnumeratorCancellation] CancellationToken ct)
    {
      ReceivedToken = ct;
      try
      {
        ct.ThrowIfCancellationRequested();
        var row = context.RowFactory.EmptyRow(inputRow, outputColumns);
        row.Values[0] = MixedValue.Int(1);
        row.Values[1] = MixedValue.GraphNode(new GraphNode<string>("root", "person",
          [new GraphEdge<string>("LINK", "target")], new Dictionary<string, object?>()));
        yield return row;
        if (!Block)
        {
          var second = context.RowFactory.EmptyRow(inputRow, outputColumns);
          second.Values[0] = MixedValue.Int(2);
          second.Values[1] = row.Values[1];
          yield return second;
          yield break;
        }
        Waiting.SetResult();
        await Task.Delay(Timeout.Infinite, ct);
      }
      finally { Disposals++; }
    }
  }

  private static IOperator Plan(string query, BlockingPlugin plugin)
  {
    ICypherParser parser = new CypherAstParser();
    parser.InstallPlugin(plugin);
    var graph = new InMemoryGraph();
    graph.AddNode("target", "person");
    graph.AddNode("root", "person");
    graph.AddEdge("root", "target", "LINK");
    return parser.ParseQuery(query).BuildQueryPlan().BuildExecutionPlan(new TestOperatorFactory(graph));
  }

  [TestCase("RETURN value")]
  [TestCase("WITH value WHERE value > 0 RETURN value")]
  [TestCase("RETURN value ORDER BY value DESC LIMIT 10")]
  [TestCase("RETURN DISTINCT value LIMIT 10")]
  [TestCase("RETURN count(*) AS total")]
  [TestCase("RETURN value, count(*) AS total")]
  [TestCase("RETURN value UNION ALL RETURN 2 AS value")]
  [TestCase("RETURN value UNION RETURN 2 AS value")]
  [TestCase("MATCH (other:person) RETURN value")]
  [TestCase("MATCH (node)-[:LINK]->(target) RETURN value")]
  [TestCase("MATCH (node)<-[:LINK]-(target) RETURN value")]
  [TestCase("MATCH (node)-[:LINK]-(target) RETURN value")]
  [TestCase("MATCH (target:person) MATCH (node)-[:LINK]->(target) RETURN value")]
  [TestCase("OPTIONAL MATCH (node)-[:MISSING]->(target) RETURN value")]
  [TestCase("CALL blocking.rows() YIELD value AS second, node AS other RETURN second")]
  public async Task EnumerationCancellationReachesProcedureThroughQueryPipeline(string suffix)
  {
    var plugin = new BlockingPlugin();
    var plan = Plan("CALL blocking.rows() YIELD value, node " + suffix, plugin);
    using var cancellation = new CancellationTokenSource();
    var consumption = Consume(plan.ExecuteAsync(new TestQueryContext()), cancellation.Token);
    await plugin.Waiting.Task.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.That(plugin.ReceivedToken.CanBeCanceled, Is.True);
    cancellation.Cancel();
    await AssertCancelled(consumption);
    Assert.That(plugin.Disposals, Is.GreaterThanOrEqualTo(1));
  }

  [TestCase(false)]
  [TestCase(true)]
  public async Task DirectTokenAndEnumerationTokenAreBothHonored(bool cancelEnumerationToken)
  {
    var plugin = new BlockingPlugin();
    var plan = Plan("CALL blocking.rows() YIELD value RETURN value", plugin);
    using var direct = new CancellationTokenSource();
    using var enumeration = new CancellationTokenSource();
    var consumption = Consume(plan.ExecuteAsync(new TestQueryContext(), direct.Token), enumeration.Token);
    await plugin.Waiting.Task.WaitAsync(TimeSpan.FromSeconds(5));
    (cancelEnumerationToken ? enumeration : direct).Cancel();
    await AssertCancelled(consumption);
    Assert.That(plugin.Disposals, Is.EqualTo(1));
  }

  [TestCase("RETURN 1 AS value")]
  [TestCase("MATCH (n:missing) RETURN n")]
  [TestCase("MATCH (n:missing) RETURN count(*) AS total")]
  [TestCase("RETURN 1 AS value LIMIT 0")]
  public async Task PreCancelledQueriesThrowInsteadOfReturningResults(string query)
  {
    var plan = Plan(query, new BlockingPlugin());
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();
    await AssertCancelled(Consume(plan.ExecuteAsync(new TestQueryContext(), cancellation.Token)));
    await AssertCancelled(Consume(plan.ExecuteAsync(new TestQueryContext()), cancellation.Token));
  }

  private static async Task Consume(IAsyncEnumerable<IRow> rows, CancellationToken ct = default)
  {
    await foreach (var row in rows.WithCancellation(ct))
    {
      Assert.That(row, Is.Not.Null);
    }
  }

  [TestCase("RETURN value ORDER BY value")]
  [TestCase("RETURN value, count(*) AS total")]
  [TestCase("RETURN DISTINCT value")]
  [TestCase("RETURN value LIMIT 10")]
  public async Task CancellationStopsBufferedOrStreamingResultsAfterFirstRow(string suffix)
  {
    var plugin = new BlockingPlugin { Block = false };
    var plan = Plan("CALL blocking.rows() YIELD value " + suffix, plugin);
    using var cancellation = new CancellationTokenSource();
    await using var rows = plan.ExecuteAsync(new TestQueryContext())
      .GetAsyncEnumerator(cancellation.Token);
    Assert.That(await rows.MoveNextAsync(), Is.True);
    cancellation.Cancel();
    await AssertCancelled(rows.MoveNextAsync().AsTask());
  }

  private static async Task AssertCancelled(Task task)
  {
    try
    {
      await task.WaitAsync(TimeSpan.FromSeconds(5));
      Assert.Fail("Query completed without observing cancellation.");
    }
    catch (OperationCanceledException)
    {
      // Cancellation must propagate without being wrapped as a query error.
    }
  }
}
