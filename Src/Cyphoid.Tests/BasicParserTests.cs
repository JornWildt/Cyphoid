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
    public void ItCanParse(string input)
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
