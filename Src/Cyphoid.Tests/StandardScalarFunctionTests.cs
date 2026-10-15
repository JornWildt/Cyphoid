using Cyphoid.Core.Exceptions;

namespace Cyphoid.Tests
{
  [TestFixture]
  internal class StandardScalarFunctionTests : TestHelper
  {
    [TestCase("coalesce(null, 'fallback')", "fallback")]
    [TestCase("coalesce(false, true)", false)]
    [TestCase("coalesce(0, 1)", 0L)]
    [TestCase("coalesce('', 'fallback')", "")]
    [TestCase("coalesce(null, null)", null)]
    [TestCase("coalesce('first', 1 / 0)", "first")]
    [TestCase("toLower('HELLO')", "hello")]
    [TestCase("TOUPPER('hello')", "HELLO")]
    [TestCase("trim('  hello  ')", "hello")]
    [TestCase("ltrim('  hello  ')", "hello  ")]
    [TestCase("rtrim('  hello  ')", "  hello")]
    [TestCase("toString(42)", "42")]
    [TestCase("toString(1.25)", "1.25")]
    [TestCase("toString(true)", "True")]
    [TestCase("toInteger('42')", 42L)]
    [TestCase("toInteger(2.5)", 2L)]
    [TestCase("toInteger(3.5)", 4L)]
    [TestCase("toInteger(true)", 1L)]
    [TestCase("toFloat('1.25')", 1.25)]
    [TestCase("toFloat(42)", 42.0)]
    [TestCase("toBoolean('TRUE')", true)]
    [TestCase("toBoolean(0)", false)]
    [TestCase("toBoolean(-1)", true)]
    [TestCase("size('hello')", 5L)]
    [TestCase("size('😀')", 2L)]
    [TestCase("size('')", 0L)]
    [TestCase("substring('abcdef', 2)", "cdef")]
    [TestCase("substring('abcdef', 2, 3)", "cde")]
    [TestCase("substring('abcdef', 6)", "")]
    [TestCase("replace('banana', 'na', 'NA')", "baNANA")]
    [TestCase("replace('Hello', 'h', 'x')", "Hello")]
    [TestCase("left('abcdef', 2)", "ab")]
    [TestCase("right('abcdef', 2)", "ef")]
    [TestCase("left('abc', 0)", "")]
    [TestCase("right('abc', 0)", "")]
    [TestCase("abs(-42)", 42L)]
    [TestCase("abs(-1.25)", 1.25)]
    [TestCase("floor(-1.25)", -2.0)]
    [TestCase("ceil(-1.25)", -1.0)]
    [TestCase("round(2.5)", 2.0)]
    [TestCase("round(3.5)", 4.0)]
    [TestCase("round(1.25, 1)", 1.2)]
    [TestCase("round(1.25, 1, 'AwayFromZero')", 1.3)]
    [TestCase("round(-1.25, 1, 'ToZero')", -1.2)]
    public async Task ScalarFunctionsReturnExpectedDotNetValues(string expression, object? expected)
    {
      var result = await ExecuteQuery($"RETURN {expression} AS value");
      Assert.That(result.Rows.Count, Is.EqualTo(1));
      Assert.That(result.Rows[0]["value"], Is.EqualTo(expected));
      if (expected != null)
        Assert.That(result.Rows[0]["value"], Is.TypeOf(expected.GetType()));
    }

    [TestCase("toLower(null)")]
    [TestCase("toUpper(null)")]
    [TestCase("trim(null)")]
    [TestCase("ltrim(null)")]
    [TestCase("rtrim(null)")]
    [TestCase("toString(null)")]
    [TestCase("toInteger(null)")]
    [TestCase("toFloat(null)")]
    [TestCase("toBoolean(null)")]
    [TestCase("size(null)")]
    [TestCase("substring(null, 0)")]
    [TestCase("substring('abc', null)")]
    [TestCase("substring('abc', 0, null)")]
    [TestCase("replace('abc', null, 'x')")]
    [TestCase("replace('abc', 'a', null)")]
    [TestCase("left('abc', null)")]
    [TestCase("right(null, 1)")]
    [TestCase("abs(null)")]
    [TestCase("floor(null)")]
    [TestCase("ceil(null)")]
    [TestCase("round(null)")]
    [TestCase("round(1.25, null)")]
    public async Task ScalarFunctionsPropagateNull(string expression)
    {
      var result = await ExecuteQuery($"RETURN {expression} AS value");
      Assert.That(result.Rows[0]["value"], Is.Null);
    }

    [TestCase("coalesce()")]
    [TestCase("trim()")]
    [TestCase("toLower('a', 'b')")]
    [TestCase("substring('abc')")]
    [TestCase("substring('abc', 0, 1, 2)")]
    [TestCase("replace('abc', 'a')")]
    [TestCase("left('abc')")]
    [TestCase("round(1, 2, 'ToEven', 3)")]
    public void ScalarFunctionsValidateArgumentCounts(string expression)
    {
      Assert.ThrowsAsync<ParseException>(async () => await ExecuteQuery($"RETURN {expression} AS value"));
    }

    [TestCase("toLower(1)")]
    [TestCase("size(false)")]
    [TestCase("toInteger('invalid')")]
    [TestCase("toInteger('999999999999999999999')")]
    [TestCase("toFloat('invalid')")]
    [TestCase("toBoolean('invalid')")]
    [TestCase("substring('abc', -1)")]
    [TestCase("substring('abc', 0, 4)")]
    [TestCase("left('abc', 4)")]
    [TestCase("right('abc', -1)")]
    [TestCase("replace('abc', '', 'x')")]
    [TestCase("round(1.25, 16)")]
    [TestCase("round(1.25, 1, 'invalid')")]
    [TestCase("abs('1')")]
    public void ScalarFunctionsReportInvalidValuesAsQueryErrors(string expression)
    {
      var error = Assert.CatchAsync<Exception>(async () => await ExecuteQuery($"RETURN {expression} AS value"));
      Assert.That(error!.GetType().Name, Is.EqualTo("RuntimeException"));
    }

    [Test]
    public async Task ScalarFunctionsComposeWithProjectionFilteringAndOptionalMatch()
    {
      Graph.AddNode("person", "person");
      Graph.SetNodeProperty("person", "name", "  ALICE  ");
      var result = await ExecuteQuery("MATCH (p:person) OPTIONAL MATCH (p)-[:knows]->(friend) WITH trim(toLower(p.name)) AS name, coalesce(friend.name, 'unknown') AS friend WHERE name STARTS WITH 'ali' RETURN name, friend");
      Assert.That(result.Rows.Count, Is.EqualTo(1));
      Assert.That(result.Rows[0]["name"], Is.EqualTo("alice"));
      Assert.That(result.Rows[0]["friend"], Is.EqualTo("unknown"));
    }
  }
}
