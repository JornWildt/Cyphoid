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


    [TestCase("MATCH (s:person) RETURN id(s) AS value", "Lisa")]
    [TestCase("MATCH (s:city) RETURN ID(s) AS value", "Holte")]
    [TestCase("MATCH (s:person) WITH id(s) AS value RETURN value", "Lisa")]
    [TestCase("MATCH (s:person) WHERE id(s) = 'Lisa' RETURN id(s) AS value", "Lisa")]
    [TestCase("MATCH (s:person) RETURN id(null) AS value", null)]
    public async Task ItReturnsNodeIds(string input, string? expected)
    {
      var result = await ExecuteQuery(input);
      Assert.That(result.Rows.Count, Is.EqualTo(1));
      Assert.That(result.Rows[0]["value"], Is.EqualTo(expected));
    }

    [TestCase("MATCH (s) RETURN id(1) AS value")]
    [TestCase("MATCH (s) RETURN id('Lisa') AS value")]
    public void IdRejectsNonNodes(string input)
    {
      var exception = Assert.CatchAsync<Exception>(async () => await ExecuteQuery(input));
      Assert.That(exception!.GetType().Name, Is.EqualTo("RuntimeException"));
      Assert.That(exception.Message, Does.Contain("id() requires a node or null"));
    }

    [TestCase("MATCH (s) RETURN id() AS value")]
    [TestCase("MATCH (s) RETURN id(s, s) AS value")]
    public void IdRequiresOneArgument(string input)
    {
      Assert.ThrowsAsync<Cyphoid.Core.Exceptions.ParseException>(async () => await ExecuteQuery(input));
    }

    [TestCase(42L)]
    [TestCase(4294967296L)]
    public void NumericNodeIdsRemainIntegers(long id)
    {
      Cyphoid.Core.Execution.IGraphNode node = new Cyphoid.Core.ReferenceBackend.GraphNode<long>(id, "person", [], []);
      Assert.That(node.GetId().Type, Is.EqualTo(MixedValue.ValueType.Int));
      Assert.That(node.GetId().AsInt(), Is.EqualTo(id));
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
