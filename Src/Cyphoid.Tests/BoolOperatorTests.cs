using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cyphoid.Tests
{
  [TestFixture]
  internal class BoolOperatorTests : TestHelper
  {
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
      Graph.AddNode("Copenhagen", "city");
      Graph.AddNode("Oslo", "city");
      Graph.AddNode("Stockholm", "city");
      Graph.AddNode("Helsinki", "city");

      Graph.AddNode("Denmark", "country");
      Graph.AddNode("Norway", "country");
      Graph.AddNode("Sweden", "country");
      Graph.AddNode("Germany", "country");
      Graph.AddNode("England", "country");

      Graph.SetNodeProperty("Copenhagen", "name", "København");
      Graph.SetNodeProperty("Copenhagen", "isDanish", true);
      Graph.AddEdge("Copenhagen", "Denmark", "located_in");

      Graph.SetNodeProperty("Oslo", "name", "Oslo");
      Graph.SetNodeProperty("Oslo", "isNorwegian", true);
      Graph.AddEdge("Oslo", "Norway", "located_in");

      Graph.SetNodeProperty("Denmark", "name", "Denmark");
      Graph.SetNodeProperty("Norway", "name", "Norway");
    }


    [TestCase("MATCH (n) WHERE n.isDanish RETURN n", 1)]
    [TestCase("MATCH (n) WHERE n.isDanish = null RETURN n", 8)]
    [TestCase("MATCH (n) WHERE n.isDanish = true RETURN n", 1)]
    [TestCase("MATCH (n) WHERE n.isDanish = false RETURN n", 0)]
    [TestCase("MATCH (n) WHERE n.isDanish RETURN n", 1)]
    [TestCase("MATCH (n) WHERE n.isDanish RETURN n", 1)]
    [TestCase("MATCH (n) WHERE NOT n.isDanish RETURN n", 8)]
    [TestCase("MATCH (n) WHERE n.name = 'Oslo' RETURN n", 1)]
    [TestCase("MATCH (n) WHERE n.name <> 'Oslo' RETURN n", 8)]
    [TestCase("MATCH (n) WHERE n.name = 'Oslo' OR n.name = 'København' RETURN n", 2)]
    [TestCase("MATCH (n) WHERE n.name = 'Oslo' AND n.name = 'København' RETURN n", 0)]
    [TestCase("MATCH (n) WHERE n.name = 'Oslo' = true RETURN n", 1)]
    [TestCase("MATCH (n) WHERE n.name <> 'Oslo' <> true RETURN n", 1)]
    public async Task ItCanHandleBooleanOperators(string input, int rowCount)
    {
      // Act
      var result = await ExecuteQuery(input);

      // Assert
      Assert.That(result.Print, Is.EqualTo(input.Replace("'", "\"")));
      Assert.That(result.Rows.Count, Is.EqualTo(rowCount));
    }
  }
}
