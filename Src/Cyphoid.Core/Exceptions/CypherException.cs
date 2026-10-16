namespace Cyphoid.Core.Exceptions
{
  public abstract class CypherException : Exception
  {
    public CypherErrorCode Code { get; protected init; }
    public CypherErrorPhase Phase { get; protected init; }
    public SourceSpan? SourceSpan { get; private set; }
    public virtual IReadOnlyList<CypherDiagnostic> Diagnostics =>
      new[] { new CypherDiagnostic(Code, Message, Phase, SourceSpan) };

    internal void Locate(SourceSpan? span) => SourceSpan ??= span;

    protected CypherException(string message, Exception innerException)
      : base(message, innerException) { }

    protected CypherException(string message)
        : base(message)
    {
    }
  }
}
