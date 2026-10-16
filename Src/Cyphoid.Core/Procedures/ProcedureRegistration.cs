using Antlr4.Runtime;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.Procedures;

public sealed record ProcedureRegistration(
  string QualifiedName,
  ICypherProcedure Procedure,
  IReadOnlyList<ProcedureField> Parameters,
  IReadOnlyList<ProcedureField> Outputs)
{
  internal static bool IsIdentifier(string value) =>
    !string.IsNullOrEmpty(value) && IsLetter(value[0]) &&
    value.All(c => IsLetter(c) || c is >= '0' and <= '9' || c == '_') &&
    new CypherLexer(new AntlrInputStream(value)).NextToken().Type == CypherLexer.IDENTIFIER;

  private static bool IsLetter(char c) => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z';

  internal static IReadOnlyList<ProcedureField> Snapshot(IReadOnlyList<ProcedureField> fields)
  {
    ArgumentNullException.ThrowIfNull(fields);
    var copy = fields.ToArray();
    if (copy.Any(f => f == null || !IsIdentifier(f.Name) || !Enum.IsDefined(f.Type)) ||
        copy.Select(f => f.Name).Distinct(StringComparer.Ordinal).Count() != copy.Length)
      throw new ArgumentException("Procedure fields must have unique identifiers and valid MixedValue types.");
    return Array.AsReadOnly(copy);
  }

  internal static bool Accepts(ProcedureField field, MixedValue value) =>
    value.IsNull() ? field.Nullable || field.Type == MixedValue.ValueType.Null : value.Type == field.Type;
}
