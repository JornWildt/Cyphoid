using Cyphoid.Core.Exceptions;

namespace Cyphoid.Core.Expressions.Functions
{
  internal static class FunctionRegistry
  {
    static readonly Dictionary<string, IFunctionDefinition> FunctionDefinitions = new(StringComparer.OrdinalIgnoreCase)
    {
      ["CountAll"] = new CountAllFunction(),
      ["Count"] = new CountFunction(),
      ["Sum"] = new SumFunction(),
      ["Min"] = new MinFunction(),
      ["Max"] = new MaxFunction(),
      ["Avg"] = new AvgFunction(),
    };


    static public IFunctionDefinition GetFunctionDefinition(string name)
    {
      if (FunctionDefinitions.TryGetValue(name, out var definition))
      {
        return definition;
      }

      throw new ParseException($"Unknown function '{name}'.");
    }
  }
}
