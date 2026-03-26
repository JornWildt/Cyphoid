using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Cyphoid.Tests
{
  [TestFixture]
  internal class WithTests : TestHelper
  {
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
      Graph.AddNode("Denmark", "country");
      Graph.AddNode("Norway", "country");

      Graph.SetNodeProperty("Denmark", "name", "Danmark");
      Graph.SetNodeProperty("Norway", "name", "Norge");

      Graph.AddNode("Copenhagen", "city");
      Graph.SetNodeProperty("Copenhagen", "name", "København");
      Graph.AddEdge("Copenhagen", "Denmark", "located_in");

      Graph.AddNode("Oslo", "city");
      Graph.SetNodeProperty("Oslo", "name", "Oslo");
      Graph.AddEdge("Oslo", "Norway", "located_in");

      Graph.AddNode("Holte", "city");
      Graph.SetNodeProperty("Holte", "name", "Holte");
      Graph.AddEdge("Holte", "Denmark", "located_in");
    }


    [TestCase("WITH 1 AS a RETURN a",
      @"[{""a"":1}]")]
    [TestCase("WITH 1 AS a WITH a AS b, 'X' AS c RETURN b, c",
      @"[{""b"":1,""c"":""X""}]")]
    [TestCase("WITH 'Norge' AS cn MATCH (c) WHERE c.name = cn RETURN c.name",
      @"[{""name"":""Norge""}]")]
    [TestCase("WITH 'Norge' AS cn MATCH (c) WHERE c.name = cn WITH c AS x RETURN x.name",
      @"[{""name"":""Norge""}]")]
    public async Task ItCanUseWith(string input, string expectedOutputJson)
    {
      // Act
      var result = await ExecuteQuery(input);
      var resultJson = JsonConvert.SerializeObject(result.Rows);

      // Assert
      Assert.That(result.Print, Is.EqualTo(input.Replace("'", "\"")));
      Assert.That(resultJson, Is.EqualTo(expectedOutputJson));
    }
  }
}
