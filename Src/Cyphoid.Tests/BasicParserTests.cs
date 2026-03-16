using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Sharpen;
using Cyphoid.Core;

namespace Cyphoid.Tests
{
  public class EvalVisitor : CypherBaseVisitor<int>
  {
    public override int VisitQuery([NotNull] CypherParser.QueryContext context)
    {
      return base.VisitQuery(context);
    }

    public override int VisitMatchClause([NotNull] CypherParser.MatchClauseContext context)
    {
      var x = Visit(context.pattern());
      return base.VisitMatchClause(context);
    }

    public override int VisitReturnClause([NotNull] CypherParser.ReturnClauseContext context)
    {
      var x = context.returnItem();
      return base.VisitReturnClause(context);
    }

    public override int VisitLimitClause([NotNull] CypherParser.LimitClauseContext context)
    {
      var x = context.integerLiteral();
      return base.VisitLimitClause(context);
    }


    public override int VisitIntegerLiteral([NotNull] CypherParser.IntegerLiteralContext context)
    {
      var s = context.INTEGER().GetText();
      var i = int.Parse(s);
      return i;
    }
  }


  [TestFixture]
  internal class BasicParserTests
  {
    [TestCase("MATCH (s:species)\r\nRETURN s\r\nLIMIT 10")]
    public void ItCanParse(string input)
    {
      ICypherParser parser = new CypherAstParser();
      var queryNode = parser.ParseQuery(input);
    }
  }
}
