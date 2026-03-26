using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Cyphoid.Tests
{
  internal class GroupAndAggregateTests : TestHelper
  {
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
      Graph.AddNode("Denmark", "country");
      Graph.AddNode("Norway", "country");
      Graph.AddNode("Sweden", "country");
      Graph.AddNode("Germany", "country");
      Graph.AddNode("England", "country");

      Graph.SetNodeProperty("Denmark", "name", "Danmark");
      Graph.SetNodeProperty("Norway", "name", "Norge");
      Graph.SetNodeProperty("Sweden", "name", "Sverige");
      Graph.SetNodeProperty("Germany", "name", "Tyskland");
      Graph.SetNodeProperty("England", "name", "England");

      Graph.AddNode("Copenhagen", "city");
      Graph.SetNodeProperty("Copenhagen", "name", "København");
      Graph.SetNodeProperty("Copenhagen", "isDanish", true);
      Graph.SetNodeProperty("Copenhagen", "population", 1000000);
      Graph.AddEdge("Copenhagen", "Denmark", "located_in");

      Graph.AddNode("Oslo", "city");
      Graph.SetNodeProperty("Oslo", "name", "Oslo");
      Graph.SetNodeProperty("Oslo", "isNorwegian", true);
      Graph.AddEdge("Oslo", "Norway", "located_in");

      Graph.AddNode("Holte", "city");
      Graph.SetNodeProperty("Holte", "name", "Holte");
      Graph.AddEdge("Holte", "Denmark", "located_in");

      Graph.AddNode("Nærum", "city");
      Graph.SetNodeProperty("Nærum", "name", "Nærum");
      Graph.AddEdge("Nærum", "Denmark", "located_in");

      Graph.AddNode("Allerød", "city");
      Graph.SetNodeProperty("Allerød", "name", "Allerød");
      Graph.AddEdge("Allerød", "Denmark", "located_in");

      Graph.AddNode("Stockholm", "city");
      Graph.SetNodeProperty("Stockholm", "name", "Stockholm");
      Graph.AddEdge("Stockholm", "Sweden", "located_in");

      Graph.AddNode("Helsingborg", "city");
      Graph.SetNodeProperty("Helsingborg", "name", "Helsingborg");
      Graph.AddEdge("Helsingborg", "Sweden", "located_in");

      Graph.AddNode("Helsinki", "city");
    }


    [TestCase("MATCH (c:country) RETURN COUNT(*) AS n",
      @"[{""n"":5}]")]
    [TestCase("MATCH (c:country)<-[]-(x) RETURN c.name, COUNT(*) AS n",
      @"[{""name"":""Danmark"",""n"":4},{""name"":""Norge"",""n"":1},{""name"":""Sverige"",""n"":2}]")]
    [TestCase("MATCH (c:country)<-[]-(x) RETURN c.name, COUNT(*) AS n ORDER BY n",
      @"[{""name"":""Norge"",""n"":1},{""name"":""Sverige"",""n"":2},{""name"":""Danmark"",""n"":4}]")]
    [TestCase("MATCH (c:country)<-[]-(x) RETURN c.name, COUNT(*) AS n ORDER BY n DESC",
      @"[{""name"":""Danmark"",""n"":4},{""name"":""Sverige"",""n"":2},{""name"":""Norge"",""n"":1}]")]
    [TestCase("MATCH (c:country)<-[]-(x) RETURN c.name, COUNT(*) AS n ORDER BY n DESC LIMIT 1",
      @"[{""name"":""Danmark"",""n"":4}]")]
    [TestCase("MATCH (c:city)-[]->(x) RETURN c.name AS cname, COUNT(*) AS n",
      @"[{""cname"":""København"",""n"":1},{""cname"":""Oslo"",""n"":1},{""cname"":""Holte"",""n"":1},{""cname"":""Nærum"",""n"":1},{""cname"":""Allerød"",""n"":1},{""cname"":""Stockholm"",""n"":1},{""cname"":""Helsingborg"",""n"":1}]")]
    [TestCase("MATCH (c:city)-[]->(x) RETURN c.name AS cname, x.name AS xname, COUNT(*) AS n",
      @"[{""cname"":""København"",""xname"":""Danmark"",""n"":1},{""cname"":""Oslo"",""xname"":""Norge"",""n"":1},{""cname"":""Holte"",""xname"":""Danmark"",""n"":1},{""cname"":""Nærum"",""xname"":""Danmark"",""n"":1},{""cname"":""Allerød"",""xname"":""Danmark"",""n"":1},{""cname"":""Stockholm"",""xname"":""Sverige"",""n"":1},{""cname"":""Helsingborg"",""xname"":""Sverige"",""n"":1}]")]
    public async Task ItCanGroupByAndCount(string input, string expectedOutputJson)
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
