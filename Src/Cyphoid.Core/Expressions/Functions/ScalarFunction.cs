using Cyphoid.Core.Exceptions;
using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Expressions.Functions
{
  internal class ScalarFunction(string name, int minimumArguments, int maximumArguments,
    Func<MixedValue[], MixedValue> evaluate) : IFunctionDefinition
  {
    public ValueKindType ValueKind => ValueKindType.Variable;

    public RowEvaluator GetRowEvaluator(IReadOnlyList<RowEvaluator> parameters)
    {
      if (parameters.Count < minimumArguments || parameters.Count > maximumArguments)
        throw new ParseException($"{name}() requires {minimumArguments} to {maximumArguments} arguments.");

      return row =>
      {
        var values = parameters.Select(p => p(row)).ToArray();
        if (values.Any(v => v.IsNull()))
          return MixedValue.Null();
        try
        {
          return evaluate(values);
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException or OverflowException or InvalidCastException)
        {
          throw new RuntimeException($"{name}(): {exception.Message}");
        }
      };
    }

    public IAggregationEvaluator GetAggregator(IReadOnlyList<RowEvaluator> parameters, int outputSlotIndex)
      => throw new NotSupportedException($"{name}() is a scalar function.");
  }

  internal class CoalesceFunction : IFunctionDefinition
  {
    public ValueKindType ValueKind => ValueKindType.Variable;

    public RowEvaluator GetRowEvaluator(IReadOnlyList<RowEvaluator> parameters)
    {
      if (parameters.Count == 0)
        throw new ParseException("coalesce() requires at least one argument.");
      return row =>
      {
        foreach (var parameter in parameters)
        {
          var value = parameter(row);
          if (!value.IsNull())
            return value;
        }
        return MixedValue.Null();
      };
    }

    public IAggregationEvaluator GetAggregator(IReadOnlyList<RowEvaluator> parameters, int outputSlotIndex)
      => throw new NotSupportedException("coalesce() is a scalar function.");
  }
}
