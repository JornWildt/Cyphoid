using Cyphoid.Core;

namespace Cyphoid.Tests
{
  [TestFixture]
  internal class BasicParserTests
  {
    [TestCase("RETURN 1")]
    [TestCase("RETURN \"Hello\"")]
    // RETURN {name: "Alice"}
    [TestCase("MATCH (n) RETURN n")]
    [TestCase("MATCH (s:species) RETURN s")]
    [TestCase("MATCH (s:species) RETURN s LIMIT 10")]
    [TestCase("MATCH (a)-[]->(b) RETURN a, b")]
    [TestCase("MATCH (a)<-[]-(b) RETURN a, b")]
    [TestCase("MATCH (a)-[]-(b) RETURN a, b")]
    [TestCase("MATCH (a)-[r]->(b) RETURN r")]
    [TestCase("MATCH ()-[:KNOWS]->() RETURN 1")]
    [TestCase("MATCH (n) WHERE n.name = \"Alice\" RETURN n")]
    [TestCase("MATCH (a), (b) RETURN a, b")]
    [TestCase("MATCH (n {name: \"Alice\"}) RETURN n")]
    [TestCase("MATCH (n:Person {name: \"Alice\"}) RETURN n")]
    // FIXME: works, but what is p-map in relationship??? [TestCase("MATCH ()-[r {since: 2020}]->() RETURN r")]
    [TestCase("MATCH ()-[:KNOWS {since: 2020}]->() RETURN 1")]
    [TestCase("MATCH (n {name: \"Alice\", age: 30}) RETURN n")]
    // FIXME: allow empty property map [TestCase("MATCH (n {}) RETURN n")]
    [TestCase("MATCH (a)-[r {weight: 5}]->(b) RETURN a, b")]
    [TestCase("MATCH (n) WHERE n AND NOT n RETURN n")]
    [TestCase("MATCH (n) RETURN n OR NOT n")]
    [TestCase("MATCH (n) MATCH (o) RETURN n")]
    [TestCase("MATCH (n) WHERE n.name = \"John\" MATCH (o) RETURN n")]
    [TestCase("MATCH (n) WHERE n.name = \"John\" MATCH (o) WHERE o.x = n.y RETURN n")]
    [TestCase("MATCH (s) RETURN s ORDER BY s")]
    [TestCase("MATCH (s) RETURN s ORDER BY s.x")]
    [TestCase("MATCH (s) RETURN s ORDER BY s ASC")]
    [TestCase("MATCH (s) RETURN s ORDER BY s DESC")]
    [TestCase("MATCH (s)-[]->(o) RETURN s, o ORDER BY s, o")]
    [TestCase("MATCH (s)-[]->(o) RETURN s, o ORDER BY s ASC, o")]
    [TestCase("MATCH (s)-[]->(o) RETURN s, o ORDER BY s, o DESC")]
    [TestCase("MATCH (s)-[]->(o) RETURN s, o ORDER BY s DESC, o ASC")]
    [TestCase("MATCH (s) RETURN s.name AS name, fn() AS c")]
    [TestCase("MATCH (s) RETURN s.name AS name, fn(s) AS c")]
    [TestCase("MATCH (s) RETURN s.name AS name, fn(s, s) AS c")]
    [TestCase("MATCH (s) RETURN s.name AS name, COUNT(*) AS c")]
    public void ItCanParseAndPrettyPrint(string input)
    {
      // Arrange
      ICypherParser parser = new CypherAstParser();

      // Act
      var queryNode = parser.ParseQuery(input);
      var output = queryNode.PrettyPrint();

      Assert.That(output, Is.EqualTo(input));
    }
  }
}
