namespace Cyphoid.Tests
{
  [TestFixture]
  internal class StringOperatorTests : TestHelper
  {
    [OneTimeSetUp]
    public void SetUpGraph()
    {
      Graph.AddNode("project", "project");
      Graph.SetNodeProperty("project", "title", "Project Alpha");
      Graph.SetNodeProperty("project", "pattern", "Alpha");
      Graph.AddNode("missing", "project");
      Graph.AddNode("number", "project");
      Graph.SetNodeProperty("number", "title", 123);
    }

    [TestCase("CONTAINS", "Project", true)]
    [TestCase("CONTAINS", "ject Al", true)]
    [TestCase("CONTAINS", "Alpha", true)]
    [TestCase("STARTS WITH", "Project", true)]
    [TestCase("STARTS WITH", "Alpha", false)]
    [TestCase("ENDS WITH", "Alpha", true)]
    [TestCase("ENDS WITH", "Project", false)]
    [TestCase("CONTAINS", "alpha", false)]
    [TestCase("STARTS WITH", "project", false)]
    [TestCase("ENDS WITH", "alpha", false)]
    [TestCase("CONTAINS", "%", false)]
    [TestCase("STARTS WITH", "Project.*", false)]
    [TestCase("ENDS WITH", ".*Alpha", false)]
    [TestCase("CONTAINS", "", true)]
    [TestCase("STARTS WITH", "", true)]
    [TestCase("ENDS WITH", "", true)]
    public async Task StringOperatorsMatchCaseSensitiveLiteralText(string op, string pattern, bool expected)
    {
      var query = $"MATCH (p) RETURN 'Project Alpha' {op} '{pattern}' AS value LIMIT 1";
      var result = await ExecuteQuery(query);
      Assert.That(result.Print, Is.EqualTo(query.Replace("'", "\"")));
      Assert.That(result.Rows[0]["value"], Is.EqualTo(expected));
    }

    [TestCase("CONTAINS")]
    [TestCase("STARTS WITH")]
    [TestCase("ENDS WITH")]
    public async Task StringOperatorsReturnNullUnlessBothOperandsAreStrings(string op)
    {
      foreach (var expression in new[] { $"null {op} 'x'", $"'x' {op} null", $"123 {op} '2'", $"'123' {op} 2", $"false {op} 'x'", $"p {op} 'x'" })
      {
        var result = await ExecuteQuery($"MATCH (p) RETURN {expression} AS value LIMIT 1");
        Assert.That(result.Rows[0]["value"], Is.Null, expression);
      }
    }

    [TestCase("contains", "Alpha")]
    [TestCase("starts with", "Project")]
    [TestCase("ends with", "Alpha")]
    public async Task WhereStringOperatorsExcludeMissingAndNonStringProperties(string op, string pattern)
    {
      var result = await ExecuteQuery($"MATCH (p) WHERE p.title {op} '{pattern}' RETURN id(p) AS id ORDER BY p.title");
      Assert.That(result.Rows.Select(r => r["id"]), Is.EqualTo(new[] { "project" }));
    }

    [TestCase("CONTAINS", true)]
    [TestCase("STARTS WITH", false)]
    [TestCase("ENDS WITH", true)]
    public async Task StringOperatorsAcceptPropertiesOnBothSides(string op, bool expected)
    {
      var result = await ExecuteQuery($"MATCH (p) WHERE id(p) = 'project' RETURN p.title {op} p.pattern AS value");
      Assert.That(result.Rows[0]["value"], Is.EqualTo(expected));
    }
  }
}
