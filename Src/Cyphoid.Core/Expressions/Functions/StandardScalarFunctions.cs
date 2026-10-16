using System.Globalization;

namespace Cyphoid.Core.Expressions.Functions
{
  internal static class StandardScalarFunctions
  {
    public static IEnumerable<KeyValuePair<string, IFunctionDefinition>> Create()
    {
      yield return new("coalesce", new CoalesceFunction());
      foreach (var (name, transform) in new (string, Func<string, string>)[] {
        ("toLower", s => s.ToLowerInvariant()), ("toUpper", s => s.ToUpperInvariant()),
        ("trim", s => s.Trim()), ("ltrim", s => s.TrimStart()), ("rtrim", s => s.TrimEnd()) })
        yield return new(name, new ScalarFunction(name, 1, 1, v => MixedValue.String(transform(v[0].AsString()))));

      yield return new("toString", new ScalarFunction("toString", 1, 1,
        v => MixedValue.String(Convert.ToString(Primitive(v[0]), CultureInfo.InvariantCulture))));
      yield return new("toInteger", new ScalarFunction("toInteger", 1, 1,
        v => MixedValue.Int(Convert.ToInt64(Primitive(v[0]), CultureInfo.InvariantCulture))));
      yield return new("toFloat", new ScalarFunction("toFloat", 1, 1,
        v => MixedValue.Double(Convert.ToDouble(Primitive(v[0]), CultureInfo.InvariantCulture))));
      yield return new("toBoolean", new ScalarFunction("toBoolean", 1, 1,
        v => MixedValue.Bool(Convert.ToBoolean(Primitive(v[0]), CultureInfo.InvariantCulture))));
      yield return new("size", new ScalarFunction("size", 1, 1, v => MixedValue.Int(v[0].AsString().Length)));
      yield return new("substring", new ScalarFunction("substring", 2, 3, v => MixedValue.String(v.Length == 2
        ? v[0].AsString().Substring(Index(v[1])) : v[0].AsString().Substring(Index(v[1]), Index(v[2])))));
      yield return new("replace", new ScalarFunction("replace", 3, 3,
        v => MixedValue.String(v[0].AsString().Replace(v[1].AsString(), v[2].AsString(), StringComparison.Ordinal))));
      yield return new("left", new ScalarFunction("left", 2, 2,
        v => MixedValue.String(v[0].AsString().Substring(0, Index(v[1])))));
      yield return new("right", new ScalarFunction("right", 2, 2, v =>
      {
        var text = v[0].AsString();
        var length = Index(v[1]);
        return MixedValue.String(text.Substring(checked(text.Length - length), length));
      }));
      yield return new("abs", new ScalarFunction("abs", 1, 1, v => v[0].TryGetInt(out var integer)
        ? MixedValue.Int(Math.Abs(integer)) : MixedValue.Double(Math.Abs(Number(v[0])))));
      yield return new("floor", new ScalarFunction("floor", 1, 1, v => MixedValue.Double(Math.Floor(Number(v[0])))));
      yield return new("ceil", new ScalarFunction("ceil", 1, 1, v => MixedValue.Double(Math.Ceiling(Number(v[0])))));
      yield return new("round", new ScalarFunction("round", 1, 3, v => MixedValue.Double(Math.Round(
        Number(v[0]), v.Length >= 2 ? Index(v[1]) : 0,
        v.Length == 3 ? Enum.Parse<MidpointRounding>(v[2].AsString(), true) : MidpointRounding.ToEven))));
    }

    private static int Index(MixedValue value) => checked((int)value.AsInt());
    private static double Number(MixedValue value) => value.TryGetInt(out var integer) ? integer : value.AsDouble();
    private static object Primitive(MixedValue value) => value.Type is MixedValue.ValueType.Node or MixedValue.ValueType.Map
      ? throw new InvalidCastException("Cannot convert a node or map to a scalar value.") : value.AsObject()!;
  }
}
