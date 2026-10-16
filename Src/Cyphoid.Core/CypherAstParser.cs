using Antlr4.Runtime;
using Cyphoid.Core.Exceptions;
using Cyphoid.Core.SyntaxTree;
using Cyphoid.Core.Procedures;

namespace Cyphoid.Core
{
  public class CypherAstParser : ICypherParser
  {
    private readonly object catalogLock = new();
    private readonly HashSet<string> namespaces = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ProcedureRegistration> procedures = new(StringComparer.Ordinal);

    public void InstallPlugin(ICypherProcedurePlugin plugin)
    {
      ArgumentNullException.ThrowIfNull(plugin);
      var ns = plugin.Namespace;
      if (string.IsNullOrEmpty(ns) || ns.Split('.').Any(p => !ProcedureRegistration.IsIdentifier(p)))
        throw new ArgumentException("A plugin namespace must contain dot-separated identifiers.", nameof(plugin));

      // Discover and validate everything before publishing any registration.
      var additions = new Dictionary<string, ProcedureRegistration>(StringComparer.Ordinal);
      var discovered = plugin.GetProcedures()
        ?? throw new ArgumentException($"Plugin '{ns}' returned a null procedure collection.", nameof(plugin));
      foreach (var procedure in discovered)
      {
        if (procedure == null || !ProcedureRegistration.IsIdentifier(procedure.Name))
          throw new ArgumentException("A procedure must have a valid name.", nameof(plugin));
        var name = ns + "." + procedure.Name;
        var registration = new ProcedureRegistration(name, procedure,
          ProcedureRegistration.Snapshot(procedure.Parameters), ProcedureRegistration.Snapshot(procedure.Outputs));
        if (registration.Outputs.Count == 0)
          throw new ArgumentException("Procedures must declare at least one output.", nameof(plugin));
        if (!additions.TryAdd(name, registration))
          throw new ArgumentException($"Duplicate procedure '{name}'.", nameof(plugin));
      }
      lock (catalogLock)
      {
        if (!namespaces.Add(ns))
          throw new ArgumentException($"Namespace '{ns}' is already installed.", nameof(plugin));
        foreach (var entry in additions)
          procedures.Add(entry.Key, entry.Value);
      }
    }

    QueryNode ICypherParser.ParseQuery(string query)
    {
      ArgumentNullException.ThrowIfNull(query);
      AntlrInputStream inputStream = new AntlrInputStream(query);
      CypherLexer lexer = new CypherLexer(inputStream);
      CommonTokenStream tokens = new CommonTokenStream(lexer);
      CypherParser parser = new CypherParser(tokens);

      var lexerErrors = new CollectingErrorListener();
      var parserErrors = new CollectingErrorListener();

      lexer.RemoveErrorListeners();
      parser.RemoveErrorListeners();

      lexer.AddErrorListener(lexerErrors);
      parser.AddErrorListener(parserErrors);

      var tree = parser.query();

      if (lexerErrors.Errors.Count > 0 || parserErrors.Errors.Count > 0)
      {
        var allErrors = lexerErrors.Diagnostics.Concat(parserErrors.Diagnostics)
          .OrderBy(d => d.SourceSpan?.Start).ToArray();
        throw new ParseException(allErrors);
      }

      Dictionary<string, ProcedureRegistration> catalog;
      lock (catalogLock)
        catalog = new(procedures, StringComparer.Ordinal);
      var visitor = new Cypher2AstNodeVisitor(catalog);
      var result = visitor.Visit<QueryNode>(tree);

      return result;
    }
  }
}
