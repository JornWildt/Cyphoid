using Cyphoid.Tests.TestBackend;

namespace Cyphoid.Tests
{
  [TestFixture]
  internal class RowMetadataTests : TestHelper
  {
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
      Graph.AddNode("Singleton", "singleton");

      Graph.AddNode("Copenhagen", "city");
      Graph.SetNodeProperty("Copenhagen", "name", "København");
      Graph.SetNodeProperty("Copenhagen", "isDanish", true);

      Graph.AddNode("Oslo", "city");
      Graph.SetNodeProperty("Oslo", "name", "Oslo");
      Graph.SetNodeProperty("Oslo", "isNorwegian", true);

      Graph.AddNode("Stockholm", "city");
      Graph.SetNodeProperty("Stockholm", "name", "Stockholm");
      Graph.SetNodeProperty("Stockholm", "isSwedish", true);
    }


    [TestCase("MATCH (c:city) RETURN c ORDER BY c.name", "Copenhagen;Oslo;Stockholm")]
    [TestCase("MATCH (c:city) RETURN c.name ORDER BY name", "Copenhagen;Oslo;Stockholm")]
    [TestCase("MATCH (s:singleton) MATCH(c:city) RETURN c ORDER BY c.name", "Singleton,Copenhagen;Singleton,Oslo;Singleton,Stockholm")]
    [TestCase("MATCH (s:singleton) MATCH(c:city) RETURN s, COUNT(c)", "")]
    public async Task ItTransfersMetadata(string input, string expectedIds)
    {
      // Arrange
      var expectedIdRows = expectedIds.Split(';', StringSplitOptions.TrimEntries);

      // Act
      var result = await BasicExecuteQuery(input);

      // Assert
      Assert.That(result.Count, Is.EqualTo(expectedIdRows.Length));

      for (int i=0; i<result.Count; ++i)
      {
        var expectedRowIds = expectedIdRows[i].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var rowIds = ((TestRow)result[i]).NodeIdsForRow;

        Assert.That(rowIds.Count, Is.EqualTo(expectedRowIds.Length));
        foreach (var rowId in expectedRowIds)
          Assert.That(rowIds.Contains(rowId), Is.True);
      }
    }
  }
}
