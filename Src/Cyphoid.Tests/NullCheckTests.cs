namespace Cyphoid.Tests
{
  [TestFixture]
  internal class NullCheckTests : TestHelper
  {
    [OneTimeSetUp]
    public void SetUpGraph()
    {
      Graph.AddNode("missing", "sample");
      Graph.AddNode("null", "sample");
      Graph.SetNodeProperty("null", "value", null);
      Graph.AddNode("zero", "sample");
      Graph.SetNodeProperty("zero", "value", 0);
      Graph.AddNode("false", "sample");
      Graph.SetNodeProperty("false", "value", false);
      Graph.AddNode("empty", "sample");
      Graph.SetNodeProperty("empty", "value", "");
    }

    [TestCase("MATCH (p) WHERE p.value IS NULL RETURN id(p) AS id ORDER BY id", new[] { "missing", "null" })]
    [TestCase("MATCH (p) WHERE p.value IS NOT NULL RETURN id(p) AS id ORDER BY id", new[] { "empty", "false", "zero" })]
    [TestCase("MATCH (p) WHERE NOT p.value IS NULL RETURN id(p) AS id ORDER BY id", new[] { "empty", "false", "zero" })]
    [TestCase("MATCH (p) WHERE p.value IS NOT NULL AND id(p) = 'zero' RETURN id(p) AS id", new[] { "zero" })]
    [TestCase("MATCH (p) WHERE p.value IS NULL OR id(p) = 'zero' RETURN id(p) AS id ORDER BY id", new[] { "missing", "null", "zero" })]
    public async Task ItFiltersNullValues(string input, string[] expected)
    {
      var result = await ExecuteQuery(input);
      Assert.That(result.Print, Is.EqualTo(input.Replace("'", "\"")));
      Assert.That(result.Rows.Select(r => r["id"]), Is.EqualTo(expected));
    }

    [TestCase("null IS NULL", true)]
    [TestCase("null IS NOT NULL", false)]
    [TestCase("0 IS NULL", false)]
    [TestCase("false IS NOT NULL", true)]
    [TestCase("'' IS NOT NULL", true)]
    [TestCase("(1 + 2) IS NOT NULL", true)]
    [TestCase("(null + 2) IS NULL", true)]
    [TestCase("p IS NOT NULL", true)]
    [TestCase("null is not null", false)]
    public async Task ItProjectsBooleanNullChecks(string expression, bool expected)
    {
      var result = await ExecuteQuery($"MATCH (p) RETURN {expression} AS value LIMIT 1");
      Assert.That(result.Rows[0]["value"], Is.TypeOf<bool>());
      Assert.That(result.Rows[0]["value"], Is.EqualTo(expected));
    }

    [Test]
    public async Task ItFiltersProjectsAndOrdersByUnreturnedNode()
    {
      Graph.AddNode("projectA", "project");
      Graph.SetNodeProperty("projectA", "project_number", 1);
      Graph.SetNodeProperty("projectA", "id", "A");
      Graph.SetNodeProperty("projectA", "title", "Alpha");
      Graph.SetNodeProperty("projectA", "project_type", "internal");
      Graph.AddNode("projectB", "project");
      Graph.SetNodeProperty("projectB", "project_number", 2);
      Graph.SetNodeProperty("projectB", "id", "B");
      Graph.SetNodeProperty("projectB", "title", "Beta");

      const string query = "MATCH (p) WHERE p.project_number IS NOT NULL RETURN p.id AS id, p.project_number AS project_number, p.title AS title, p.project_type AS project_type ORDER BY p.title";
      var result = await ExecuteQuery(query);

      Assert.That(result.Print, Is.EqualTo(query));
      Assert.That(result.Rows.Select(r => r["id"]), Is.EqualTo(new[] { "A", "B" }));
      Assert.That(result.Rows[0]["project_number"], Is.EqualTo(1L));
      Assert.That(result.Rows[0]["project_type"], Is.EqualTo("internal"));
      Assert.That(result.Rows[1]["project_type"], Is.Null);
      Assert.That(result.Rows.All(r => r.Count == 4), Is.True);
    }
  }
}
