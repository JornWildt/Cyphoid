using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core
{
  enum VariableKindType { Node }

  record VariableDefinition(string Name, VariableKindType Kind, int SlotIndex);


  internal class Cypher2AstNodeVisitor : CypherBaseVisitor<AstNode>
  {
    int AnonymousVariableCounter = 1;
    Dictionary<string, VariableDefinition> VariableDefinitions = new Dictionary<string, VariableDefinition>();


    public override AstNode VisitQuery([NotNull] global::CypherParser.QueryContext context)
    {
      var match = Visit<MatchNode>(context.matchClause());
      ExtractVariableDefinitions(match);

      var limit = Visit<LimitNode>(context.limitClause());
      return new QueryNode(null!, null, null!, null);
    }


    public override AstNode VisitMatchClause([NotNull] CypherParser.MatchClauseContext context)
    {
      return new MatchNode(Visit<PatternNode>(context.pattern()));
    }


    public override AstNode VisitPattern([NotNull] CypherParser.PatternContext context)
    {
      var parts = new List<PatternPartNode>();

      foreach (var partCtx in context.patternPart())
      {
        parts.Add(Visit<PatternPartNode>(partCtx));
      }

      return new PatternNode(parts);
    }


    public override AstNode VisitPatternPart([NotNull] CypherParser.PatternPartContext context)
    {
      var rootPattern = Visit<NodePatternNode>(context.nodePattern());
      var chain = new List<PatternChainNode>() { new PatternChainNode(null, rootPattern) };

      foreach (var next in context.patternChain())
      {
        var c = Visit<PatternChainNode>(next);
        chain.Add(c);
      }

      return new PatternPartNode(chain);
    }


    public override AstNode VisitNodePattern([NotNull] CypherParser.NodePatternContext context)
    {
      var variable = Visit<VariableNode>(context.variable());
      return new NodePatternNode(variable.Name, null, null);
    }


    public override AstNode VisitPatternChain([NotNull] CypherParser.PatternChainContext context)
    {
      //var relPattern = Visit<RelationshipPatternNode>(context.relationshipPattern());
      return new PatternChainNode(null, null!);
    }


    public override AstNode VisitLimitClause([NotNull] global::CypherParser.LimitClauseContext context)
    {
      var value = (IntLiteralNode)Visit(context.integerLiteral());
      return base.VisitLimitClause(context);
    }


    public override AstNode VisitVariable([NotNull] CypherParser.VariableContext context)
    {
      var name = context.identifier().GetText();
      return new VariableNode(name);
    }


    public T Visit<T>(IParseTree tree) where T : AstNode
    {
      var result = base.Visit(tree);
      if (result is not T)
        throw new InvalidOperationException($"Unexpected type '{result?.GetType()}' - expected '{typeof(T)}'.");
      return (T)result;
    }


    void ExtractVariableDefinitions(MatchNode match)
    {
      foreach (var p in match.Pattern.Parts)
      {
        foreach (var c in p.PatternChain)
        {
          ExtractVariableDefinition(c.NodePattern);
        }
      }
    }

    
    private void ExtractVariableDefinition(NodePatternNode nodePattern)
    {
      string variableName = nodePattern.Variable ?? NewAnonymousVariable();
      if (VariableDefinitions.TryGetValue(variableName, out var variableDefinition))
      {
        if (variableDefinition.Kind != VariableKindType.Node)
          throw new InvalidOperationException($"Variable '{variableName}' reused as '{variableDefinition.Kind}' - expected '{VariableKindType.Node}'.");
      }
      else
      {
        var v = new VariableDefinition(variableName, VariableKindType.Node, VariableDefinitions.Count);
        VariableDefinitions.Add(variableName, v);
      }
    }


    private string NewAnonymousVariable()
    {
      string name = "anon_" + AnonymousVariableCounter++;
      return name;
    }
  }
}
