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


    [TestCase("WITH 1 AS A")]
    public async Task ItCanUseWith(string input)
    {
      // Act
      var result = await ExecuteQuery(input);

      // Assert
      Assert.That(result.Print, Is.EqualTo(input.Replace("'", "\"")));

    }
  }
}
