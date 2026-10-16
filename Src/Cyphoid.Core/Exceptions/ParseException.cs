namespace Cyphoid.Core.Exceptions
{
  public class ParseException : CypherException
  {
    private readonly IReadOnlyList<CypherDiagnostic>? diagnostics;

    public override IReadOnlyList<CypherDiagnostic> Diagnostics => diagnostics ?? base.Diagnostics;

    public ParseException(string message, CypherErrorCode code = CypherErrorCode.InvalidQuery)
      : base(message)
    {
      Code = code;
      Phase = CypherErrorPhase.Parsing;
    }

    public ParseException(IReadOnlyList<CypherDiagnostic> diagnostics)
      : this(string.Join(Environment.NewLine, diagnostics.Select(d =>
        $"line {d.SourceSpan?.Line}:{d.SourceSpan?.Column} {d.Message}")),
        diagnostics.Count > 0 ? diagnostics[0].Code : CypherErrorCode.InvalidQuery)
    {
      this.diagnostics = Array.AsReadOnly(diagnostics.ToArray());
      Locate(diagnostics.FirstOrDefault()?.SourceSpan);
    }
  }
}
