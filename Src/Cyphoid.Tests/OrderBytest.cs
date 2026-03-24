using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Cyphoid.Tests
{
  [TestFixture]
  internal class OrderBytest : TestHelper
  {
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
      Graph.AddNode("Lisa", "person");
      Graph.AddNode("Mona", "person");
      Graph.AddNode("Martin", "person");

      Graph.SetNodeProperty("Lisa", "Name", "Lisa Nilson");
      Graph.SetNodeProperty("Mona", "Name", "Mona Marius");
      Graph.SetNodeProperty("Martin", "Name", "Martin Mollerup");

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
      Graph.AddNode("Helsinki", "city");
    }


    [TestCase("MATCH (n) RETURN n.name ORDER BY name", "name", 15, true)]
    [TestCase("MATCH (n) RETURN n.name ORDER BY name ASC", "name", 15, true)]
    [TestCase("MATCH (n) RETURN n.name ORDER BY name DESC", "name", 15, false)]
    public async Task ItCanOrderBy(string input, string columnName, int rowCount, bool ascending)
    {
      // Act
      var result = await ExecuteQuery(input);

      // Assert
      Assert.That(result.Print, Is.EqualTo(input.Replace("'", "\"")));
      Assert.That(result.Rows.Count, Is.EqualTo(rowCount));

      var first = result.Rows[0][columnName] ?? "";
      for (int i = 1; i < result.Rows.Count; i++)
      {
        if (ascending)
          Assert.That(first, Is.LessThanOrEqualTo(result.Rows[i][columnName] ?? ""));
        else
          Assert.That(first, Is.GreaterThanOrEqualTo(result.Rows[i][columnName] ?? ""));
        first = result.Rows[i][columnName] ?? "";
      }
    }


    [TestCase("MATCH (ci:city) MATCH (co:country) RETURN ci.name AS ciname, co.name AS coname ORDER BY ciname, coname", "ciname", "coname", 35, null)]
    [TestCase("MATCH (ci:city) MATCH (co:country) RETURN ci.name AS ciname, co.name AS coname ORDER BY coname, ciname", "coname", "ciname", 35, null)]
    [TestCase("MATCH (ci:city) MATCH (co:country) RETURN ci.name AS ciname, co.name AS coname ORDER BY coname, ciname LIMIT 3", "coname", "ciname", 3,
              "[{\"ciname\":null,\"coname\":\"Danmark\"},{\"ciname\":null,\"coname\":\"Danmark\"},{\"ciname\":\"Allerød\",\"coname\":\"Danmark\"}]")]
    public async Task ItCanOrderByMultiple(string input, string columnName1, string columnName2, int rowCount, string? expectedJson)
    {
      // Act
      var result = await ExecuteQuery(input);

      // Assert
      Assert.That(result.Print, Is.EqualTo(input.Replace("'", "\"")));
      Assert.That(result.Rows.Count, Is.EqualTo(rowCount));

      if (expectedJson == null)
      {
        var orderedResult = result.Rows
          .OrderBy(r => r[columnName1])
          .ThenBy(r => r[columnName2])
          .ToList();

        for (int i = 0; i < result.Rows.Count; i++)
        {
          Assert.That(result.Rows[i][columnName1], Is.EqualTo(orderedResult[i][columnName1]));
          Assert.That(result.Rows[i][columnName2], Is.EqualTo(orderedResult[i][columnName2]));
        }
      }
      else
      {
        var json = JsonConvert.SerializeObject(result.Rows);
        Assert.That(json, Is.EqualTo(expectedJson));
      }
    }
  }
}
