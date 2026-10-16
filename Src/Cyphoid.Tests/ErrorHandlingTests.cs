using Cyphoid.Core;
using Cyphoid.Core.Exceptions;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;
using Cyphoid.Core.Procedures;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Tests.TestBackend;

namespace Cyphoid.Tests;

[TestFixture]
internal class ErrorHandlingTests : TestHelper
{
  [TestCase("count()")]
  [TestCase("count(1, 2)")]
  [TestCase("sum()")]
  [TestCase("sum(1, 2)")]
  [TestCase("min()")]
  [TestCase("max(1, 2)")]
  [TestCase("avg()")]
  [TestCase("avg(1, 2)")]
  [TestCase("type()")]
  [TestCase("labels(1, 2)")]
  [TestCase("rand(1)")]
  [TestCase("id()")]
  [TestCase("substring('a')")]
  [TestCase("coalesce()")]
  [TestCase("min(*)")]
  [TestCase("rand(*)")]
  public void InvalidFunctionSignaturesFailDuringParsing(string expression)
  {
    ICypherParser parser = new CypherAstParser();
    var error = Assert.Throws<ParseException>(() => parser.ParseQuery("RETURN " + expression));
    Assert.That(error!.Code, Is.EqualTo(CypherErrorCode.InvalidArgumentCount));
    Assert.That(error.Phase, Is.EqualTo(CypherErrorPhase.Parsing));
    Assert.That(error.SourceSpan, Is.Not.Null);
    Assert.That(error.SourceSpan!.Start, Is.EqualTo(7));
  }

  [TestCase("RETURN count(*) + 1")]
  [TestCase("RETURN count(*) IS NULL")]
  [TestCase("RETURN -count(*)")]
  [TestCase("RETURN NOT count(*)")]
  [TestCase("RETURN count(*) IN [1]")]
  [TestCase("RETURN 1 IN [count(*)]")]
  [TestCase("RETURN coalesce(count(*), 0)")]
  [TestCase("RETURN sum(count(*))")]
  [TestCase("MATCH (n) WHERE count(*) > 0 RETURN n")]
  [TestCase("RETURN 1 AS n ORDER BY count(*)")]
  public void InvalidAggregatePlacementFailsDuringParsing(string query)
  {
    ICypherParser parser = new CypherAstParser();
    var error = Assert.Throws<ParseException>(() => parser.ParseQuery(query));
    Assert.That(error!.Code, Is.EqualTo(CypherErrorCode.InvalidAggregateUsage));
  }

  [TestCase("RETURN missing", CypherErrorCode.UnknownVariable)]
  [TestCase("RETURN unknown(1)", CypherErrorCode.UnknownFunction)]
  [TestCase("CALL absent.proc() YIELD value RETURN value", CypherErrorCode.UnknownProcedure)]
  [TestCase("RETURN 1 LIMIT 2147483648", CypherErrorCode.InvalidArgumentValue)]
  public void SemanticErrorsHaveStableCodesAndLocations(string query, CypherErrorCode code)
  {
    ICypherParser parser = new CypherAstParser();
    var error = Assert.Throws<ParseException>(() => parser.ParseQuery(query));
    Assert.That(error!.Code, Is.EqualTo(code));
    Assert.That(error.Diagnostics, Has.Count.EqualTo(1));
    Assert.That(error.Diagnostics[0].SourceSpan, Is.Not.Null);
  }

  [Test]
  public void SyntaxErrorsRetainIndividualDiagnosticsInSourceOrder()
  {
    ICypherParser parser = new CypherAstParser();
    var error = Assert.Throws<ParseException>(() => parser.ParseQuery("RETURN @1 AS x,\n@2 AS y"));
    Assert.That(error!.Code, Is.EqualTo(CypherErrorCode.SyntaxError));
    Assert.That(error.Diagnostics, Has.Count.EqualTo(2));
    Assert.That(error.Diagnostics[0].SourceSpan, Is.EqualTo(new SourceSpan(7, 1, 1, 7)));
    Assert.That(error.Diagnostics[1].SourceSpan!.Line, Is.EqualTo(2));
  }

  [TestCase("1 / 0", CypherErrorCode.DivisionByZero)]
  [TestCase("1 % 0", CypherErrorCode.DivisionByZero)]
  [TestCase("9223372036854775807 + 1", CypherErrorCode.NumericOverflow)]
  [TestCase("(-9223372036854775807 - 1) - 1", CypherErrorCode.NumericOverflow)]
  [TestCase("9223372036854775807 * 2", CypherErrorCode.NumericOverflow)]
  [TestCase("-(-9223372036854775807 - 1)", CypherErrorCode.NumericOverflow)]
  [TestCase("(-9223372036854775807 - 1) / -1", CypherErrorCode.NumericOverflow)]
  public void ArithmeticFailuresArePublicRuntimeErrors(string expression, CypherErrorCode code)
  {
    var error = Assert.ThrowsAsync<RuntimeException>(async () => await ExecuteQuery("RETURN " + expression));
    Assert.That(error!.Code, Is.EqualTo(code));
    Assert.That(error.Phase, Is.EqualTo(CypherErrorPhase.Execution));
    Assert.That(error.InnerException, Is.Not.Null);
    Assert.That(error.SourceSpan, Is.Not.Null);
  }

  [Test]
  public void ScalarErrorsPreserveCauseAndLocateFunction()
  {
    var error = Assert.ThrowsAsync<RuntimeException>(async () =>
      await ExecuteQuery("RETURN\nsubstring('a', 0, 9) AS value"));
    Assert.That(error!.Code, Is.EqualTo(CypherErrorCode.InvalidArgumentValue));
    Assert.That(error.InnerException, Is.InstanceOf<ArgumentOutOfRangeException>());
    Assert.That(error.Message, Does.Contain("substring()"));
    Assert.That(error.SourceSpan, Is.EqualTo(new SourceSpan(7, 20, 2, 0)));
  }

  [Test]
  public void TypeErrorsIncludeFunctionContextAndKeepInnerException()
  {
    var error = Assert.ThrowsAsync<RuntimeException>(async () => await ExecuteQuery("RETURN toLower(1)"));
    Assert.That(error!.Code, Is.EqualTo(CypherErrorCode.TypeMismatch));
    Assert.That(error.Message, Does.Contain("toLower()"));
    Assert.That(error.InnerException, Is.TypeOf<RuntimeException>());
  }

  [Test]
  public void RuntimeLocationPointsToInnerFailingExpression()
  {
    var error = Assert.ThrowsAsync<RuntimeException>(async () => await ExecuteQuery("RETURN 1 + (2 / 0)"));
    Assert.That(error!.SourceSpan, Is.EqualTo(new SourceSpan(12, 5, 1, 12)));
  }

  [Test]
  public void AggregateArithmeticErrorsHaveFunctionContext()
  {
    Graph.AddNode("a", "number");
    Graph.AddNode("b", "number");
    Graph.SetNodeProperty("a", "value", long.MaxValue);
    Graph.SetNodeProperty("b", "value", 1L);
    var error = Assert.ThrowsAsync<RuntimeException>(async () =>
      await ExecuteQuery("MATCH (n:number) RETURN sum(n.value)"));
    Assert.That(error!.Code, Is.EqualTo(CypherErrorCode.NumericOverflow));
    Assert.That(error.Message, Does.Contain("sum()"));
    Assert.That(error.SourceSpan, Is.Not.Null);
  }

  private sealed class FailurePlugin : ICypherProcedurePlugin, ICypherProcedure
  {
    public string Namespace => "failures";
    public string Name => "rows";
    public IReadOnlyList<ProcedureField> Parameters => [];
    public IReadOnlyList<ProcedureField> Outputs => [new("value", MixedValue.ValueType.Int)];
    public IEnumerable<ICypherProcedure> GetProcedures() => [this];
    public Exception? MoveFailure { get; init; }
    public Exception? CleanupFailure { get; init; }
    public bool InvalidResult { get; init; }
    public bool SynchronousFailure { get; init; }

    public IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context, IRow inputRow,
      IReadOnlyList<MixedValue> arguments, RowSchema outputColumns, CancellationToken ct)
    {
      if (SynchronousFailure) throw new InvalidOperationException("Invocation failed");
      var row = context.RowFactory.EmptyRow(inputRow, outputColumns);
      row.Values[0] = InvalidResult ? MixedValue.String("bad") : MixedValue.Int(1);
      return new Results(this, row);
    }

    private sealed class Results(FailurePlugin owner, IRow row) : IAsyncEnumerable<IRow>, IAsyncEnumerator<IRow>
    {
      private bool emitted;
      public IRow Current => row;
      public IAsyncEnumerator<IRow> GetAsyncEnumerator(CancellationToken ct = default) => this;
      public ValueTask<bool> MoveNextAsync()
      {
        if (owner.MoveFailure != null) return ValueTask.FromException<bool>(owner.MoveFailure);
        if (emitted) return ValueTask.FromResult(false);
        emitted = true;
        return ValueTask.FromResult(true);
      }
      public ValueTask DisposeAsync() => owner.CleanupFailure == null ? ValueTask.CompletedTask
        : ValueTask.FromException(owner.CleanupFailure);
    }
  }

  private static IAsyncEnumerable<IRow> FailureQuery(FailurePlugin plugin, IQueryContext? context = null)
  {
    ICypherParser parser = new CypherAstParser();
    parser.InstallPlugin(plugin);
    return parser.ParseQuery("CALL failures.rows() YIELD value RETURN value")
      .BuildQueryPlan().BuildExecutionPlan(new TestOperatorFactory(new InMemoryGraph()))
      .ExecuteAsync(context ?? new TestQueryContext());
  }

  [Test]
  public void CleanupFailureDoesNotReplaceExecutionFailure()
  {
    var primary = new InvalidOperationException("Primary failure");
    var cleanup = new InvalidOperationException("Cleanup failure");
    var error = Assert.ThrowsAsync<RuntimeException>(async () => await FailureQuery(new FailurePlugin
      { MoveFailure = primary, CleanupFailure = cleanup }).ToListAsync());
    Assert.That(error!.Code, Is.EqualTo(CypherErrorCode.ProcedureFailed));
    Assert.That(error.InnerException, Is.SameAs(primary));
    Assert.That(CypherExceptionDiagnostics.GetCleanupExceptions(error), Is.EqualTo(new[] { cleanup }));
    Assert.That(error.SourceSpan!.Start, Is.Zero);
  }

  [Test]
  public void CleanupFailureDoesNotReplaceCancellation()
  {
    var primary = new OperationCanceledException();
    var cleanup = new InvalidOperationException("Cleanup failure");
    var error = Assert.ThrowsAsync<OperationCanceledException>(async () => await FailureQuery(new FailurePlugin
      { MoveFailure = primary, CleanupFailure = cleanup }).ToListAsync());
    Assert.That(error, Is.SameAs(primary));
    Assert.That(CypherExceptionDiagnostics.GetCleanupExceptions(error!), Is.EqualTo(new[] { cleanup }));
  }

  [Test]
  public void CleanupCancellationDoesNotReplaceQueryFailure()
  {
    var cleanup = new OperationCanceledException();
    var error = Assert.ThrowsAsync<RuntimeException>(async () => await FailureQuery(new FailurePlugin
      { MoveFailure = new InvalidOperationException("Primary"), CleanupFailure = cleanup }).ToListAsync());
    Assert.That(CypherExceptionDiagnostics.GetCleanupExceptions(error!), Is.EqualTo(new[] { cleanup }));
  }

  [Test]
  public void InvalidResultIsDistinctFromProcedureFailureEvenWhenCleanupFails()
  {
    var cleanup = new InvalidOperationException("Cleanup failure");
    var error = Assert.ThrowsAsync<RuntimeException>(async () => await FailureQuery(new FailurePlugin
      { InvalidResult = true, CleanupFailure = cleanup }).ToListAsync());
    Assert.That(error!.Code, Is.EqualTo(CypherErrorCode.InvalidProcedureResult));
    Assert.That(CypherExceptionDiagnostics.GetCleanupExceptions(error), Is.EqualTo(new[] { cleanup }));
  }

  [Test]
  public void CleanupOnlyFailureIsReported()
  {
    var cleanup = new InvalidOperationException("Cleanup failure");
    var error = Assert.ThrowsAsync<RuntimeException>(async () => await FailureQuery(new FailurePlugin
      { CleanupFailure = cleanup }).ToListAsync());
    Assert.That(error!.InnerException, Is.SameAs(cleanup));
    Assert.That(CypherExceptionDiagnostics.GetCleanupExceptions(error), Is.Empty);
  }

  [Test]
  public void SynchronousPluginFailuresAreWrappedWithContext()
  {
    var error = Assert.ThrowsAsync<RuntimeException>(async () => await FailureQuery(new FailurePlugin
      { SynchronousFailure = true }).ToListAsync());
    Assert.That(error!.Code, Is.EqualTo(CypherErrorCode.ProcedureFailed));
    Assert.That(error.Message, Does.Contain("failures.rows"));
    Assert.That(error.InnerException, Is.TypeOf<InvalidOperationException>());
  }

  private sealed class FailingMergeContext(Exception failure) : IQueryContext, IQueryRowFactory
  {
    public IQueryRowFactory RowFactory => this;
    public IRow EmptyRow(RowSchema columns) => new FailingRow(columns, failure);
    public IRow EmptyRow(IRow source, RowSchema columns) => EmptyRow(columns);

    private sealed class FailingRow(RowSchema columns, Exception failure) : Row(columns)
    {
      protected override IRow MergeWith(IRow other) => throw failure;
    }
  }

  [Test]
  public void HostRowFailuresKeepTheirIdentityEvenWhenProcedureCleanupFails()
  {
    var primary = new InvalidOperationException("Host merge failed");
    var cleanup = new InvalidOperationException("Cleanup failure");
    var error = Assert.ThrowsAsync<InvalidOperationException>(async () => await FailureQuery(new FailurePlugin
      { CleanupFailure = cleanup }, new FailingMergeContext(primary)).ToListAsync());
    Assert.That(error, Is.SameAs(primary));
    Assert.That(CypherExceptionDiagnostics.GetCleanupExceptions(error!), Is.EqualTo(new[] { cleanup }));
  }

  [Test]
  public async Task EarlyExitReportsCleanupFailure()
  {
    var cleanup = new InvalidOperationException("Cleanup failure");
    var results = FailureQuery(new FailurePlugin { CleanupFailure = cleanup }).GetAsyncEnumerator();
    Assert.That(await results.MoveNextAsync(), Is.True);
    var error = Assert.ThrowsAsync<RuntimeException>(async () => await results.DisposeAsync());
    Assert.That(error!.Code, Is.EqualTo(CypherErrorCode.ProcedureFailed));
    Assert.That(error.InnerException, Is.SameAs(cleanup));
  }
}
