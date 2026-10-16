using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Sharpen;
using Cyphoid.Core.Exceptions;

namespace Cyphoid.Core
{
  public class CollectingErrorListener : IParserErrorListener, IAntlrErrorListener<int>
  {
    public List<string> Errors { get; } = new();
    public List<CypherDiagnostic> Diagnostics { get; } = new();

    void IAntlrErrorListener<IToken>.SyntaxError(
      TextWriter output,
      IRecognizer recognizer,
      IToken offendingSymbol,
      int line,
      int charPositionInLine,
      string msg,
      RecognitionException e)
    {
      Errors.Add($"line {line}:{charPositionInLine} {msg}");
      Diagnostics.Add(new(CypherErrorCode.SyntaxError, msg, CypherErrorPhase.Parsing,
        new SourceSpan(Math.Max(0, offendingSymbol.StartIndex),
          Math.Max(0, offendingSymbol.StopIndex - offendingSymbol.StartIndex + 1), line, charPositionInLine)));
    }

    void IAntlrErrorListener<int>.SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
    {
      Errors.Add($"line {line}:{charPositionInLine} {msg}");
      var start = e is LexerNoViableAltException lexerError ? lexerError.StartIndex : recognizer.InputStream.Index;
      Diagnostics.Add(new(CypherErrorCode.SyntaxError, msg, CypherErrorPhase.Parsing,
        new SourceSpan(Math.Max(0, start), 1, line, charPositionInLine)));
    }

    void IParserErrorListener.ReportAmbiguity(Parser recognizer, DFA dfa, int startIndex, int stopIndex, bool exact, BitSet ambigAlts, ATNConfigSet configs)
    {
    }

    void IParserErrorListener.ReportAttemptingFullContext(Parser recognizer, DFA dfa, int startIndex, int stopIndex, BitSet conflictingAlts, ATNConfigSet configs)
    {
    }

    void IParserErrorListener.ReportContextSensitivity(Parser recognizer, DFA dfa, int startIndex, int stopIndex, int prediction, ATNConfigSet configs)
    {
    }
  }
}
