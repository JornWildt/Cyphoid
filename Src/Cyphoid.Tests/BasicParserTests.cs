using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Sharpen;
using Cyphoid.Core;

namespace Cyphoid.Tests
{
  [TestFixture]
  internal class BasicParserTests
  {
    // FIXME: MATCH is optional
    // [TestCase("RETURN 1")]
    // [TestCase("RETURN \"Hello\"")]
    // RETURN {name: "Alice"}
    [TestCase("MATCH (n) RETURN n")]
    [TestCase("MATCH (s:species) RETURN s")]
    [TestCase("MATCH (s:species) RETURN s LIMIT 10")]
    [TestCase("MATCH (a)-[]->(b) RETURN a, b")]
    [TestCase("MATCH (a)<-[]-(b) RETURN a, b")]
    [TestCase("MATCH (a)-[]-(b) RETURN a, b")]
    [TestCase("MATCH (a)-[r]->(b) RETURN r")]
    [TestCase("MATCH ()-[:KNOWS]->() RETURN 1")]
    // FIXME: not implemented [TestCase("MATCH (n) WHERE n.name = \"Alice\" RETURN n")]
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
    [TestCase("MATCH (n {name: x OR NOT x}) RETURN n OR NOT n")]
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
