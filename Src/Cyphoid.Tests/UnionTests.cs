using Cyphoid.Core;
using Cyphoid.Core.Exceptions;

namespace Cyphoid.Tests
{
  [TestFixture]
  internal class UnionTests : TestHelper
  {
    [OneTimeSetUp]
    public void SetUpGraph()
    {
      Graph.AddNode("A", "person");
      Graph.AddNode("B", "person");
    }

    [TestCase("RETURN 1 AS value UNION RETURN 1 AS value", new long[] { 1 })]
    [TestCase("RETURN 1 AS value UNION ALL RETURN 1 AS value", new long[] { 1, 1 })]
    [TestCase("RETURN 1 AS value UNION RETURN 2 AS value UNION RETURN 1 AS value", new long[] { 1, 2 })]
    [TestCase("RETURN 1 AS value UNION ALL RETURN 2 AS value UNION ALL RETURN 1 AS value", new long[] { 1, 2, 1 })]
    [TestCase("MATCH (n:absent) RETURN 0 AS value UNION RETURN 2 AS value", new long[] { 2 })]
    [TestCase("MATCH (n:person) RETURN 1 AS value UNION RETURN 2 AS value", new long[] { 1, 2 })]
    public async Task UnionCombinesRowsWithRequestedDuplicateBehavior(string query, long[] expected)
    {
      var result = await ExecuteQuery(query);
      Assert.That(result.Print, Is.EqualTo(query));
      Assert.That(result.Rows.Select(r => r["value"]), Is.EquivalentTo(expected));
    }

    [TestCase("RETURN null AS value UNION RETURN null AS value", 1)]
    [TestCase("RETURN null AS value UNION ALL RETURN null AS value", 2)]
    [TestCase("MATCH (n:person) RETURN n UNION MATCH (n:person) RETURN n", 2)]
    [TestCase("MATCH (n:person) RETURN n UNION ALL MATCH (n:person) RETURN n", 4)]
    [TestCase("RETURN 1 AS a, 2 AS b UNION RETURN 1 AS a, 3 AS b", 2)]
    [TestCase("RETURN 1 AS a, 2 AS b UNION RETURN 1 AS a, 2 AS b", 1)]
    [TestCase("MATCH (n:absent) RETURN n UNION MATCH (m:absent) RETURN m AS n", 0)]
    public async Task UnionDistinctComparesCompleteRowsIncludingNodesAndNulls(string query, int count)
    {
      var result = await ExecuteQuery(query);
      Assert.That(result.Rows.Count, Is.EqualTo(count));
    }

    [Test]
    public async Task BranchesHaveIndependentSlotsAndLocalOrderingAndLimits()
    {
      var result = await ExecuteQuery("MATCH (n:person) RETURN id(n) AS value ORDER BY value DESC LIMIT 1 UNION ALL MATCH (m:person) WITH m AS x RETURN id(x) AS value ORDER BY value LIMIT 1");
      Assert.That(result.Rows.Select(r => r["value"]), Is.EquivalentTo(new[] { "B", "A" }));
    }

    [TestCase("RETURN 1 AS a UNION RETURN 1 AS b")]
    [TestCase("RETURN 1 AS a UNION RETURN 1 AS a, 2 AS b")]
    [TestCase("MATCH (n) RETURN n UNION RETURN n")]
    [TestCase("RETURN 1 AS a UNION RETURN 2 AS a UNION ALL RETURN 3 AS a")]
    public void InvalidUnionBranchesRaiseParseErrors(string query)
    {
      ICypherParser parser = new CypherAstParser();
      Assert.Throws<ParseException>(() => parser.ParseQuery(query));
    }

    [Test]
    public async Task ExplicitDistinctAndLowercaseUnionAreAccepted()
    {
      var result = await ExecuteQuery("RETURN 1 AS value union distinct RETURN 1 AS value");
      Assert.That(result.Rows.Count, Is.EqualTo(1));
      Assert.That(result.Print, Is.EqualTo("RETURN 1 AS value UNION RETURN 1 AS value"));
    }
  }
}
