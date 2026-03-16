using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core
{
  public class CypherAstParser : ICypherParser
  {
    QueryNode ICypherParser.ParseQuery(string query) 
    {
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
        var allErrors = lexerErrors.Errors.Concat(parserErrors.Errors);
        throw new Exception(string.Join(Environment.NewLine, allErrors));
      }

      var visitor = new Cypher2AstNodeVisitor();
      var result = visitor.Visit<QueryNode>(tree);

      return result;
    }
  }
}
