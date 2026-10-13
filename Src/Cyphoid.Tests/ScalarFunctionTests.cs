using Cyphoid.Core.Expressions;

namespace Cyphoid.Tests
{
  [TestFixture]
  internal class ScalarFunctionTests : TestHelper
  {
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
      Graph.AddNode("Lisa", "person");
      Graph.AddNode("Holte", "city");
    }


    [TestCase("MATCH (o) RETURN rand() AS value")]
    [TestCase("MATCH (o) RETURN RAND() AS value")]
    [TestCase("MATCH (o) WITH rand() AS value RETURN value")]
    public async Task ItReturnsRandomNumbersInRange(string input)
    {
      for (var iteration = 0; iteration < 100; iteration++)
      {
        var result = await ExecuteQuery(input);

        Assert.That(result.Print, Is.EqualTo(input));
        Assert.That(result.Rows.Count, Is.EqualTo(2));
        foreach (var row in result.Rows)
        {
          Assert.That(row["value"], Is.TypeOf<double>());
          Assert.That((double)row["value"]!, Is.GreaterThanOrEqualTo(0.0).And.LessThan(1.0));
        }
      }
    }


    [TestCase("MATCH (o:person) RETURN type(o) AS value", "person")]
    [TestCase("MATCH (o:city) RETURN type(o) AS value", "city")]
    public async Task ItCanGetTypeOfNode(string input, object expectedValue)
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
