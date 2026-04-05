using Cyphoid.Core.Expressions;

namespace Cyphoid.Tests
{
  [TestFixture]
  internal class MathTests : TestHelper
  {
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
      Graph.AddNode("Int0", "number");
      Graph.SetNodeProperty("Int0", "value", 0);
      
      Graph.AddNode("Int12", "number");
      Graph.SetNodeProperty("Int12", "value", 12);
    }


    [TestCase("MATCH (n {value: 0}) RETURN n.value", 0)]
    [TestCase("MATCH (n {value: 0}) RETURN n.value + 2 AS value", 2)]
    [TestCase("MATCH (n {value: 0}) RETURN n.value - 2 AS value", -2)]
    [TestCase("MATCH (n {value: 0}) RETURN n.value * 2 AS value", 0)]
    [TestCase("MATCH (n {value: 0}) RETURN n.value / 2 AS value", 0)]
    [TestCase("MATCH (n {value: 0}) RETURN n.value % 2 AS value", 0)]
    [TestCase("MATCH (n {value: 12}) RETURN n.value + 2 AS value", 14)]
    [TestCase("MATCH (n {value: 12}) RETURN n.value - 2 AS value", 10)]
    [TestCase("MATCH (n {value: 12}) RETURN n.value * 2 AS value", 24)]
    [TestCase("MATCH (n {value: 12}) RETURN n.value / 2 AS value", 6)]
    [TestCase("MATCH (n {value: 12}) RETURN n.value % 2 AS value", 0)]
    [TestCase("MATCH (n {value: 12}) RETURN n.value % 5 AS value", 2)]
    [TestCase("MATCH (n {value: 12}) RETURN n.value + 2 * 2 AS value", 16)]
    [TestCase("MATCH (n {value: 12}) RETURN n.value - 2 * 2 AS value", 8)]
    [TestCase("MATCH (n {value: 12}) RETURN n.value * 2 * 2 AS value", 48)]
    [TestCase("MATCH (n {value: 12}) RETURN n.value / 2 * 2 AS value", 12)]
    [TestCase("MATCH (n {value: 12}) RETURN n.value % 2 * 2 AS value", 0)]
    [TestCase("MATCH (n {value: 12}) RETURN n.value % 5 * 2 AS value", 4)]
    [TestCase("MATCH (n {value: 12}) RETURN (n.value + 2) * 2 AS value", 28)]
    [TestCase("MATCH (n {value: 12}) RETURN (n.value - 2) * 2 AS value", 20)]
    public async Task ItCanCalculateBasicMath(string input, object expectedValue)
    {
      // Act
      var result = await ExecuteQuery(input);

      // Assert
      Assert.That(result.Print, Is.EqualTo(input.Replace("'", "\"")));

      Assert.That(result.Rows.Count, Is.EqualTo(1));
      Assert.That(result.Rows[0]["value"], Is.EqualTo(MixedValue.FromObject(expectedValue)));
    }
  }
}
