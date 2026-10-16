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

    public void ValidateArguments(int count)
    {
      if (count < minimumArguments || count > maximumArguments)
        throw new ParseException($"{name}() requires {minimumArguments} to {maximumArguments} arguments.", CypherErrorCode.InvalidArgumentCount);
    }

    public RowEvaluator GetRowEvaluator(IReadOnlyList<RowEvaluator> parameters)
    {
      ValidateArguments(parameters.Count);

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
          var code = exception is OverflowException ? CypherErrorCode.NumericOverflow
            : exception is InvalidCastException ? CypherErrorCode.TypeMismatch : CypherErrorCode.InvalidArgumentValue;
          throw new RuntimeException($"{name}(): {exception.Message}", exception, code);
        }
        catch (RuntimeException exception)
        {
          throw new RuntimeException($"{name}(): {exception.Message}", exception, exception.Code);
        }
      };
    }

    public IAggregationEvaluator GetAggregator(IReadOnlyList<RowEvaluator> parameters, int outputSlotIndex)
      => throw new NotSupportedException($"{name}() is a scalar function.");
  }

  internal class CoalesceFunction : IFunctionDefinition
  {
    public ValueKindType ValueKind => ValueKindType.Variable;

    public void ValidateArguments(int count)
    {
      if (count == 0)
        throw new ParseException("coalesce() requires at least one argument.", CypherErrorCode.InvalidArgumentCount);
    }

    public RowEvaluator GetRowEvaluator(IReadOnlyList<RowEvaluator> parameters)
    {
      ValidateArguments(parameters.Count);
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
