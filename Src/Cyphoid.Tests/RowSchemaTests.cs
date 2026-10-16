using System.Runtime.CompilerServices;
using Cyphoid.Core;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;
using Cyphoid.Core.Procedures;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Tests.TestBackend;

namespace Cyphoid.Tests;

[TestFixture]
internal class RowSchemaTests : TestHelper
{
  private sealed class MutableColumn : IRowColumn
  {
    public int Index { get; set; }
    public string Name { get; set; } = "value";
    public MixedValue.ValueType Type { get; set; } = MixedValue.ValueType.Int;
  }

  [Test]
  public void SchemaSnapshotsBothCollectionAndMutableColumnDefinitions()
  {
    var column = new MutableColumn();
    var input = new IRowColumn[] { column };
    var schema = new RowSchema(input);
    column.Name = "changed";
    column.Type = MixedValue.ValueType.Map;
    input[0] = new RowColumn(0, "replacement", MixedValue.ValueType.Bool);
    Assert.That(schema[0].Name, Is.EqualTo("value"));
    Assert.That(schema[0].Type, Is.EqualTo(MixedValue.ValueType.Int));
    Assert.That(schema.Columns, Is.SameAs(schema));
    Assert.That(schema.Columns, Is.Not.InstanceOf<IRowColumn[]>());
    Assert.That(schema.Columns, Is.Not.InstanceOf<IList<IRowColumn>>());
    Assert.That(schema[0], Is.Not.SameAs(column));
  }

  [Test]
  public void RowsCopiesAndMergesShareSchemasButHaveIndependentValueArrays()
  {
    var schema = new RowSchema([new RowColumn(0, "value", MixedValue.ValueType.Int)]);
    IRow first = new TestRow(new HashSet<string> { "first" }, schema);
    IRow second = new TestRow(new HashSet<string> { "second" }, schema);
    first.Values[0] = MixedValue.Int(1);
    second.Values[0] = MixedValue.Int(2);
    var copy = first.CopyAndExpand(schema);
    var merged = first.MergeWith(second);
    foreach (var row in new[] { first, second, copy, merged })
    {
      Assert.That(row.Schema, Is.SameAs(schema));
      Assert.That(row.Columns, Is.SameAs(schema.Columns));
    }
    Assert.That(copy.Values, Is.Not.SameAs(first.Values));
    Assert.That(merged.Values, Is.Not.SameAs(second.Values));
    Assert.That(copy.Values[0], Is.EqualTo(MixedValue.Int(1)));
    Assert.That(merged.Values[0], Is.EqualTo(MixedValue.Int(2)));
    Assert.That(((TestRow)merged).NodeIdsForRow, Is.EquivalentTo(new[] { "first", "second" }));
  }

  [TestCase("MATCH (n:person) RETURN n")]
  [TestCase("MATCH (n:person) RETURN DISTINCT {id: id(n)} AS person")]
  [TestCase("MATCH (n:person) RETURN n.name AS name, count(*) AS total")]
  [TestCase("MATCH (n:person) WITH n.name AS name WHERE name IS NOT NULL RETURN name ORDER BY name LIMIT 10")]
  [TestCase("MATCH (n:person) OPTIONAL MATCH (n)-[:LINK]->(other) RETURN n, other")]
  [TestCase("MATCH (n:person), (other:person) RETURN n, other")]
  [TestCase("RETURN 1 AS value UNION ALL RETURN {name: 'Alice'} AS value")]
  [TestCase("RETURN 1 AS value UNION RETURN 2 AS value")]
  [TestCase("MATCH (n:missing) RETURN n AS value UNION ALL MATCH (n:person) RETURN n AS value")]
  public async Task QueryResultsShareOneImmutableSchema(string query)
  {
    Graph.AddNode("Alice", "person");
    Graph.AddNode("Bob", "person");
    Graph.SetNodeProperty("Alice", "name", "Alice");
    Graph.SetNodeProperty("Bob", "name", "Bob");
    Graph.AddEdge("Alice", "Bob", "LINK");
    var rows = await BasicExecuteQuery(query);
    Assert.That(rows.Count, Is.GreaterThanOrEqualTo(2));
    foreach (var row in rows)
    {
      Assert.That(row.Schema, Is.SameAs(rows[0].Schema));
      Assert.That(row.Columns, Is.SameAs(rows[0].Columns));
      Assert.That(row.Values.Length, Is.EqualTo(row.Schema.Count));
    }
  }

  [Test]
  public async Task UnionSchemaNormalizationPreservesHostMetadataAndValues()
  {
    Graph.AddNode("Alice", "person");
    Graph.AddNode("Bob", "person");
    var rows = await BasicExecuteQuery("MATCH (n:person) WHERE id(n) = 'Alice' RETURN n UNION ALL MATCH (n:person) WHERE id(n) = 'Bob' RETURN n");
    Assert.That(rows, Has.Count.EqualTo(2));
    Assert.That(rows[1].Schema, Is.SameAs(rows[0].Schema));
    Assert.That(rows[0].Values[0]!.Value.AsGraphNode().GetId(), Is.EqualTo(MixedValue.String("Alice")));
    Assert.That(rows[1].Values[0]!.Value.AsGraphNode().GetId(), Is.EqualTo(MixedValue.String("Bob")));
    Assert.That(((TestRow)rows[0]).NodeIdsForRow, Is.EquivalentTo(new[] { "Alice" }));
    Assert.That(((TestRow)rows[1]).NodeIdsForRow, Is.EquivalentTo(new[] { "Bob" }));
  }

  [Test]
  public async Task BackendScanAndExpansionReuseTheMatchSchema()
  {
    Graph.AddNode("Alice", "person");
    Graph.AddNode("Bob", "person");
    Graph.AddEdge("Alice", "Bob", "LINK");
    ICypherParser parser = new CypherAstParser();
    var ast = parser.ParseQuery("MATCH (n:person) OPTIONAL MATCH (n)-[:LINK]->(other) RETURN n, other");
    var plan = ast.Clauses[1].BuildQueryPlan(ast.Clauses[0].BuildQueryPlan(null))
      .BuildExecutionPlan(new TestOperatorFactory(Graph));
    var rows = await plan.ExecuteAsync(new TestQueryContext()).ToListAsync();
    Assert.That(rows, Has.Count.EqualTo(2));
    Assert.That(rows[1].Schema, Is.SameAs(rows[0].Schema));
  }

  private sealed class SchemaPlugin : ICypherProcedurePlugin, ICypherProcedure
  {
    public string Namespace => "schema";
    public string Name => "rows";
    public IReadOnlyList<ProcedureField> Parameters => [];
    public IReadOnlyList<ProcedureField> Outputs => [new("value", MixedValue.ValueType.Int)];
    public List<RowSchema> ReceivedSchemas { get; } = [];
    public IEnumerable<ICypherProcedure> GetProcedures() => [this];

    public async IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context, IRow inputRow,
      IReadOnlyList<MixedValue> arguments, RowSchema outputColumns,
      [EnumeratorCancellation] CancellationToken ct)
    {
      ReceivedSchemas.Add(outputColumns);
      await Task.Yield();
      ct.ThrowIfCancellationRequested();
      for (var i = 0; i < 2; i++)
      {
        var row = context.RowFactory.EmptyRow(inputRow, outputColumns);
        Assert.That(row.Schema, Is.SameAs(outputColumns));
        row.Values[0] = MixedValue.Int(i);
        yield return row;
      }
    }
  }

  [Test]
  public async Task ProcedureSchemaIsSharedAcrossInvocationsAndExecutions()
  {
    Graph.AddNode("Alice", "person");
    Graph.AddNode("Bob", "person");
    var plugin = new SchemaPlugin();
    ICypherParser parser = new CypherAstParser();
    parser.InstallPlugin(plugin);
    var plan = parser.ParseQuery("MATCH (n:person) CALL schema.rows() YIELD value RETURN n, value")
      .BuildQueryPlan().BuildExecutionPlan(new TestOperatorFactory(Graph));
    var first = await plan.ExecuteAsync(new TestQueryContext()).ToListAsync();
    var second = await plan.ExecuteAsync(new TestQueryContext()).ToListAsync();
    Assert.That(first, Has.Count.EqualTo(4));
    Assert.That(second[0].Schema, Is.SameAs(first[0].Schema));
    Assert.That(plugin.ReceivedSchemas, Has.Count.EqualTo(4));
    Assert.That(plugin.ReceivedSchemas.All(s => ReferenceEquals(s, plugin.ReceivedSchemas[0])), Is.True);
  }

  [Test]
  public void InvalidSchemasAreRejectedAtConstruction()
  {
    Assert.Throws<ArgumentException>(() => new RowSchema([new RowColumn(1, "value", MixedValue.ValueType.Int)]));
    Assert.Throws<ArgumentException>(() => new RowSchema([
      new RowColumn(0, "value", MixedValue.ValueType.Int), new RowColumn(1, "value", MixedValue.ValueType.Int)]));
    Assert.That(RowSchema.Empty.Count, Is.Zero);
  }
}
