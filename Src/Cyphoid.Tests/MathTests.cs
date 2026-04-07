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
    
    
    [TestCase("RETURN 10 < 20 AS value", true)]
    [TestCase("RETURN 10 <= 20 AS value", true)]
    [TestCase("RETURN 10 > 20 AS value", false)]
    [TestCase("RETURN 10 >= 20 AS value", false)]
    [TestCase("RETURN 10 = 20 AS value", false)]
    [TestCase("RETURN 10 <> 20 AS value", true)]
    [TestCase("RETURN 10 < 10 AS value", false)]
    [TestCase("RETURN 10 <= 10 AS value", true)]
    [TestCase("RETURN 10 > 10 AS value", false)]
    [TestCase("RETURN 10 = 10 AS value", true)]
    [TestCase("RETURN 10 <> 10 AS value", false)]
    [TestCase("RETURN 10 >= 10 AS value", true)]
    [TestCase("RETURN 10.1 < 20.2 AS value", true)]
    [TestCase("RETURN 10.1 <= 20.2 AS value", true)]
    [TestCase("RETURN 10.1 > 20.2 AS value", false)]
    [TestCase("RETURN 10.1 >= 20.2 AS value", false)]
    [TestCase("RETURN 10.1 < 10.1 AS value", false)]
    [TestCase("RETURN 10.1 <= 10.1 AS value", true)]
    [TestCase("RETURN 10.1 > 10.1 AS value", false)]
    [TestCase("RETURN 10.1 >= 10.1 AS value", true)]
    [TestCase("RETURN 10.1 = 20.1 AS value", false)]
    [TestCase("RETURN 10.1 <> 20.1 AS value", true)]
    [TestCase("RETURN 'a' < 'b' AS value", true)]
    [TestCase("RETURN 'a' <= 'b' AS value", true)]
    [TestCase("RETURN 'a' > 'b' AS value", false)]
    [TestCase("RETURN 'a' >= 'b' AS value", false)]
    [TestCase("RETURN 'a' < 'a' AS value", false)]
    [TestCase("RETURN 'a' <= 'a' AS value", true)]
    [TestCase("RETURN 'a' > 'a' AS value", false)]
    [TestCase("RETURN 'a' >= 'a' AS value", true)]
    [TestCase("RETURN 'a' = 'b' AS value", false)]
    [TestCase("RETURN 'a' <> 'b' AS value", true)]
    [TestCase("RETURN 'a' = 'a' AS value", true)]
    [TestCase("RETURN 'a' <> 'a' AS value", false)]
    public async Task ItCanCompareValues(string input, bool expectedValue)
    {
      // Act
      var result = await ExecuteQuery(input);

      // Assert
      Assert.That(result.Print, Is.EqualTo(input.Replace("'", "\"")));

      Assert.That(result.Rows.Count, Is.EqualTo(1));
      Assert.That(result.Rows[0]["value"], Is.EqualTo(MixedValue.Bool(expectedValue)));
    }


    [TestCase("RETURN -1 AS value", -1)]
    [TestCase("RETURN --1 AS value", 1)]
    [TestCase("RETURN ---1 AS value", -1)]
    [TestCase("RETURN +1 AS value", 1)]
    [TestCase("RETURN ++1 AS value", 1)]
    [TestCase("RETURN +++1 AS value", 1)]
    [TestCase("RETURN -+1 AS value", -1)]
    [TestCase("RETURN +-1 AS value", -1)]
    [TestCase("RETURN -2.2 AS value", -2.2)]
    [TestCase("RETURN --2.2 AS value", 2.2)]
    [TestCase("RETURN ---2.2 AS value", -2.2)]
    [TestCase("RETURN +2.2 AS value", 2.2)]
    [TestCase("RETURN ++2.2 AS value", 2.2)]
    [TestCase("RETURN +++2.2 AS value", 2.2)]
    [TestCase("RETURN -+2.2 AS value", -2.2)]
    [TestCase("RETURN +-2.2 AS value", -2.2)]
    public async Task ItCanHandleUnaryOperators(string input, object expectedValue)
    {
      // Act
      var result = await ExecuteQuery(input);

      // Assert
      Assert.That(result.Print, Is.EqualTo(input.Replace("'", "\"")));

      Assert.That(result.Rows.Count, Is.EqualTo(1));
      Assert.That(result.Rows[0]["value"], Is.EqualTo(MixedValue.FromObject(expectedValue)));
    }


    [TestCase("RETURN 10 + 10 + 10 AS value", 30)]
    [TestCase("RETURN 10 - 10 - 10 AS value", -10)]
    [TestCase("RETURN 10 * 10 * 10 AS value", 1000)]
    [TestCase("RETURN 60 / 10 / 2 AS value", 3)]
    public async Task ItCanHandleSequenceOfIdenticalOperators(string input, object expectedValue)
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
