using System.Runtime.CompilerServices;
using Cyphoid.Core;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;
using Cyphoid.Core.Procedures;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Tests.TestBackend;

namespace Cyphoid.Tests;

[TestFixture]
public class ProcedureTests
{
  private sealed class Plugin(params ICypherProcedure[] procedures) : ICypherProcedurePlugin
  {
    public string Namespace { get; init; } = "example";
    public int Discoveries { get; private set; }
    public IEnumerable<ICypherProcedure> GetProcedures()
    {
      Discoveries++;
      return procedures;
    }
  }

  private sealed class Procedure : ICypherProcedure
  {
    public string Name { get; init; } = "numbers";
    public IReadOnlyList<ProcedureField> Parameters { get; set; } =
      new[] { new ProcedureField("start", MixedValue.ValueType.Int) };
    public IReadOnlyList<ProcedureField> Outputs { get; set; } =
      new[] { new ProcedureField("value", MixedValue.ValueType.Int),
        new ProcedureField("detail", MixedValue.ValueType.String, true) };
    public int Calls { get; private set; }
    public int Disposals { get; private set; }
    public bool Empty { get; init; }
    public bool Throw { get; init; }
    public bool WrongWidth { get; init; }
    public bool WrongType { get; init; }
    public bool WrongHiddenType { get; init; }
    public bool NullOutput { get; init; }
    public bool Cancel { get; init; }
    public bool ReceivedCancellation { get; private set; }

    public async IAsyncEnumerable<IRow> ExecuteAsync(
      IQueryContext context, IRow inputRow, IReadOnlyList<MixedValue> arguments,
      RowSchema outputColumns,
      [EnumeratorCancellation] CancellationToken ct)
    {
      Calls++;
      ReceivedCancellation = ct.CanBeCanceled;
      try
      {
        await Task.Yield();
        ct.ThrowIfCancellationRequested();
        if (Cancel) throw new OperationCanceledException(ct);
        if (Throw) throw new InvalidOperationException("Plugin failure");
        if (Empty) yield break;
        for (var i = 0; i < 2; i++)
        {
          var row = context.RowFactory.EmptyRow(inputRow, WrongWidth ? RowSchema.Empty : outputColumns);
          ((TestRow)row).NodeIdsForRow.Add("procedure-metadata");
          if (!WrongWidth)
          {
            row.Values[0] = NullOutput ? MixedValue.Null() : WrongType
              ? MixedValue.String("invalid") : MixedValue.Int((arguments[0].IsNull() ? 0 : arguments[0].AsInt()) + i);
            row.Values[1] = WrongHiddenType ? MixedValue.Int(1) : MixedValue.Null();
          }
          yield return row;
        }
      }
      finally { Disposals++; }
    }
  }

  private static ICypherParser Parser(Procedure procedure)
  {
    ICypherParser parser = new CypherAstParser();
    parser.InstallPlugin(new Plugin(procedure));
    return parser;
  }

  private static async Task<List<IRow>> Execute(ICypherParser parser, string query, InMemoryGraph? graph = null)
  {
    var ast = parser.ParseQuery(query);
    var execution = ast.BuildQueryPlan().BuildExecutionPlan(new TestOperatorFactory(graph ?? new InMemoryGraph()));
    return await execution.ExecuteAsync(new TestQueryContext()).ToListAsync();
  }

  [Test]
  public async Task LeadingCallFiltersAliasesSortsAndLimits()
  {
    var procedure = new Procedure();
    var parser = Parser(procedure);
    const string query = "CALL example.numbers(3) YIELD value AS number, detail WHERE number > 3 RETURN number, detail ORDER BY number DESC LIMIT 1";
    Assert.That(parser.ParseQuery(query).PrettyPrint(), Is.EqualTo(query));
    var rows = await Execute(parser, query);
    Assert.That(rows, Has.Count.EqualTo(1));
    Assert.That(rows[0].Values[0], Is.EqualTo(MixedValue.Int(4)));
    Assert.That(rows[0].Values[1]!.Value.IsNull(), Is.True);
    Assert.That(procedure.Calls, Is.EqualTo(1));
  }

  [Test]
  public async Task CallsPerInputRowAndPreservesBothSourcesOfMetadata()
  {
    var graph = new InMemoryGraph();
    graph.AddNode("Alice", "person");
    graph.AddNode("Bob", "person");
    var procedure = new Procedure();
    var rows = await Execute(Parser(procedure),
      "MATCH (n:person) CALL example.numbers(1) YIELD detail, value RETURN n, value, detail", graph);
    Assert.That(rows, Has.Count.EqualTo(4));
    Assert.That(procedure.Calls, Is.EqualTo(2));
    foreach (var row in rows)
    {
      var nodeId = row.Values[0]!.Value.AsGraphNode().GetId().AsString();
      Assert.That(((TestRow)row).NodeIdsForRow, Is.EquivalentTo(new[] { nodeId, "procedure-metadata" }));
      Assert.That(row.Values[1]!.Value.Type, Is.EqualTo(MixedValue.ValueType.Int));
    }
  }

  [Test]
  public async Task SupportsWithChainedCallsAggregationAndUnion()
  {
    var parser = Parser(new Procedure());
    var rows = await Execute(parser,
      "WITH 1 AS start CALL example.numbers(start) YIELD value CALL example.numbers(value) YIELD value AS next RETURN count(*) AS total UNION ALL CALL example.numbers(2) YIELD value RETURN count(*) AS total");
    Assert.That(rows.Select(r => r.Values[0]), Is.EqualTo(new[] { MixedValue.Int(4), MixedValue.Int(2) }));
  }

  [Test]
  public async Task EmptyResultsRemoveInputRows()
  {
    var rows = await Execute(Parser(new Procedure { Empty = true }),
      "CALL example.numbers(0) YIELD value RETURN value");
    Assert.That(rows, Is.Empty);
  }

  [TestCase("CALL absent.numbers(1) YIELD value RETURN value")]
  [TestCase("CALL example.Numbers(1) YIELD value RETURN value")]
  [TestCase("CALL example.numbers() YIELD value RETURN value")]
  [TestCase("CALL example.numbers(1, 2) YIELD value RETURN value")]
  [TestCase("CALL example.numbers(count(*)) YIELD value RETURN value")]
  [TestCase("CALL example.numbers(coalesce(count(*), 0)) YIELD value RETURN value")]
  [TestCase("CALL example.numbers(count(*) + 1) YIELD value RETURN value")]
  [TestCase("CALL example.numbers(1) YIELD missing RETURN missing")]
  [TestCase("CALL example.numbers(1) YIELD value, detail AS value RETURN value")]
  [TestCase("WITH 1 AS value CALL example.numbers(value) YIELD value RETURN value")]
  [TestCase("CALL example.numbers(1) YIELD value RETURN detail")]
  [TestCase("CALL example.numbers(1) YIELD * RETURN value")]
  [TestCase("CALL example.numbers(1) RETURN value")]
  public void InvalidCallsFailDuringParsing(string query)
  {
    Assert.Throws<Cyphoid.Core.Exceptions.ParseException>(() => Parser(new Procedure()).ParseQuery(query));
  }

  [TestCase("null")]
  [TestCase("'wrong'")]
  [TestCase("1.5")]
  public void InvalidRuntimeArgumentsDoNotInvokeProcedure(string argument)
  {
    var procedure = new Procedure();
    var exception = Assert.CatchAsync(async () => await Execute(Parser(procedure),
      $"CALL example.numbers({argument}) YIELD value RETURN value"));
    Assert.That(exception!.Message, Does.Contain("example.numbers").And.Contain("argument 'start'"));
    Assert.That(procedure.Calls, Is.Zero);
  }

  [TestCase("width")]
  [TestCase("type")]
  [TestCase("hidden")]
  [TestCase("null")]
  [TestCase("throw")]
  public void InvalidResultsAndPluginFailuresHaveContext(string failure)
  {
    var procedure = new Procedure
    {
      WrongWidth = failure == "width", WrongType = failure == "type",
      WrongHiddenType = failure == "hidden", NullOutput = failure == "null", Throw = failure == "throw"
    };
    var exception = Assert.CatchAsync(async () => await Execute(Parser(procedure),
      "CALL example.numbers(1) YIELD value RETURN value"));
    Assert.That(exception!.Message, Does.Contain("example.numbers"));
    Assert.That(exception.InnerException, Is.Not.Null);
    Assert.That(procedure.Disposals, Is.EqualTo(1));
  }

  [Test]
  public void CancellationIsNotWrapped()
  {
    Assert.ThrowsAsync<OperationCanceledException>(async () => await Execute(Parser(new Procedure { Cancel = true }),
      "CALL example.numbers(1) YIELD value RETURN value"));
  }

  [Test]
  public async Task EnumerationDisposesProcedureOnEarlyExitAndPassesCancellation()
  {
    var procedure = new Procedure();
    var ast = Parser(procedure).ParseQuery("CALL example.numbers(1) YIELD value RETURN value");
    var plan = ast.BuildQueryPlan()
      .BuildExecutionPlan(new TestOperatorFactory(new InMemoryGraph()));
    using var cancellation = new CancellationTokenSource();
    await foreach (var row in plan.ExecuteAsync(new TestQueryContext()).WithCancellation(cancellation.Token))
    {
      Assert.That(row.Values[0], Is.EqualTo(MixedValue.Int(1)));
      break;
    }
    Assert.That(procedure.Disposals, Is.EqualTo(1));
    Assert.That(procedure.ReceivedCancellation, Is.True);
  }

  [Test]
  public async Task RegistrationSnapshotsSignaturesAndDoesNotExecuteDuringParsing()
  {
    var procedure = new Procedure();
    var plugin = new Plugin(procedure);
    ICypherParser parser = new CypherAstParser();
    parser.InstallPlugin(plugin);
    var ast = parser.ParseQuery("CALL example.numbers(1) YIELD value RETURN value");
    procedure.Parameters = [];
    procedure.Outputs = [];
    parser.ParseQuery("CALL example.numbers(1) YIELD value RETURN value");
    Assert.That(plugin.Discoveries, Is.EqualTo(1));
    Assert.That(procedure.Calls, Is.Zero);
    parser.InstallPlugin(new Plugin(new Procedure()) { Namespace = "other" });
    var rows = await ast.BuildQueryPlan().BuildExecutionPlan(new TestOperatorFactory(new InMemoryGraph()))
      .ExecuteAsync(new TestQueryContext()).ToListAsync();
    Assert.That(rows, Has.Count.EqualTo(2));
  }

  [Test]
  public void RejectsDuplicateNamespacesAndProceduresAtomically()
  {
    ICypherParser parser = new CypherAstParser();
    Assert.Throws<ArgumentException>(() => parser.InstallPlugin(new Plugin(new Procedure(), new Procedure())));
    parser.InstallPlugin(new Plugin(new Procedure()));
    Assert.Throws<ArgumentException>(() => parser.InstallPlugin(new Plugin(new Procedure())));
    Assert.Throws<ArgumentException>(() => parser.InstallPlugin(new Plugin(new Procedure()) { Namespace = "bad-name" }));
    Assert.Throws<ArgumentException>(() => parser.InstallPlugin(new Plugin(new Procedure()) { Namespace = "return" }));
    Assert.Throws<ArgumentException>(() => parser.InstallPlugin(new Plugin(new Procedure { Name = "yield" }) { Namespace = "reserved" }));
    Assert.Throws<ArgumentException>(() => parser.InstallPlugin(new Plugin(new Procedure
    {
      Outputs = [new("value", MixedValue.ValueType.Int), new("value", MixedValue.ValueType.Int)]
    }) { Namespace = "duplicates" }));
    Assert.Throws<ArgumentException>(() => parser.InstallPlugin(new Plugin(new Procedure { Outputs = [] }) { Namespace = "empty" }));
    ICypherParser unconfigured = new CypherAstParser();
    Assert.Throws<Cyphoid.Core.Exceptions.ParseException>(() => unconfigured.ParseQuery(
      "CALL example.numbers(1) YIELD value RETURN value"));
  }

  [Test]
  public async Task NullableParametersAndOutputsAreSupported()
  {
    var procedure = new Procedure
    {
      Parameters = [new("start", MixedValue.ValueType.Int, true)],
      Outputs = [new("value", MixedValue.ValueType.Int, true), new("detail", MixedValue.ValueType.String, true)],
      NullOutput = true
    };
    var rows = await Execute(Parser(procedure), "CALL example.numbers(null) YIELD value RETURN value");
    Assert.That(rows, Has.Count.EqualTo(2));
    Assert.That(rows.All(r => r.Values[0]!.Value.IsNull()), Is.True);
  }

  private sealed class IdentityProcedure : ICypherProcedure
  {
    public string Name => "identity";
    public IReadOnlyList<ProcedureField> Parameters => [new("input", MixedValue.ValueType.Node)];
    public IReadOnlyList<ProcedureField> Outputs => [new("node", MixedValue.ValueType.Node)];
    public async IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context, IRow inputRow,
      IReadOnlyList<MixedValue> arguments, RowSchema outputColumns,
      [EnumeratorCancellation] CancellationToken ct)
    {
      await Task.Yield();
      ct.ThrowIfCancellationRequested();
      var row = context.RowFactory.EmptyRow(inputRow, outputColumns);
      row.Values[0] = arguments[0];
      yield return row;
    }
  }

  [Test]
  public async Task YieldedNodesCanBeUsedInSubsequentMatch()
  {
    ICypherParser parser = new CypherAstParser();
    parser.InstallPlugin(new Plugin(new IdentityProcedure()) { Namespace = "company.knowledge" });
    var graph = new InMemoryGraph();
    graph.AddNode("Alice", "person");
    var rows = await Execute(parser,
      "MATCH (n:person) CALL company.knowledge.identity(n) YIELD node AS copy MATCH (copy:person) RETURN id(copy) AS name", graph);
    Assert.That(rows, Has.Count.EqualTo(1));
    Assert.That(rows[0].Values[0], Is.EqualTo(MixedValue.String("Alice")));
  }
}
