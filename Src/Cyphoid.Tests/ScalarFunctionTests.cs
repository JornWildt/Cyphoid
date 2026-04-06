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
