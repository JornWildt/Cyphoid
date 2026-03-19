using Cyphoid.Core.Expressions;

namespace Cyphoid.Tests
{
  [TestFixture]
  internal class MixedValueTests
  {
    [TestCase(true, true, true)]
    [TestCase(false, false, true)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(true, null, false)]
    [TestCase(null, true, false)]
    [TestCase(false, null, false)]
    [TestCase(null, false, false)]
    [TestCase(1, 1, true)]
    [TestCase(1, 0, false)]
    [TestCase(null, 0, false)]
    [TestCase(1, null, false)]
    [TestCase("a", "a", true)]
    [TestCase("a", "b", false)]
    [TestCase(null, "b", false)]
    [TestCase("a", null, false)]
    [TestCase(null, null, true)]
    public void ItCanEvaluateEquals(object? a, object? b, bool expectEqual)
    {
      // Arrange
      var va = MixedValue.FromObject(a);
      var vb = MixedValue.FromObject(b);

      // Act
      var areEqual = va.Equals(vb);

      // Assert
      if (expectEqual)
        Assert.That(areEqual, Is.True);
      else
        Assert.That(areEqual, Is.False);
    }

    [Test]
    public void ItCanCreateNullFromString()
    {
      // Act
      //var n = MixedValue.GraphNode(null);
    }
  }
}
