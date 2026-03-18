using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core
{
  public enum VariableKindType { Node }

  public record VariableDefinition(bool IsAnonymous, string Name, VariableKindType Kind, int SlotIndex);


  internal class Cypher2AstNodeVisitor : CypherBaseVisitor<AstNode>
  {
    int AnonymousVariableCounter = 1;
    Dictionary<string, VariableDefinition> VariableDefinitions = new Dictionary<string, VariableDefinition>();


    #region Visitors

    public override AstNode VisitQuery([NotNull] global::CypherParser.QueryContext context)
    {
      var match = context.matchClause() != null ? Visit<MatchNode>(context.matchClause()) : null;

      var where = context.whereClause() != null ? Visit<WhereNode>(context.whereClause()) : null;

      var @return = Visit<ReturnNode>(context.returnClause());

      var limit = context.limitClause() != null ? Visit<LimitNode>(context.limitClause()) : null;

      return new QueryNode(match, where, @return, limit, VariableDefinitions);
    }


    public override AstNode VisitMatchClause([NotNull] CypherParser.MatchClauseContext context)
    {
      return new MatchNode(Visit<PatternNode>(context.pattern()));
    }

    public override AstNode VisitWhereClause([NotNull] CypherParser.WhereClauseContext context)
    {
      var expr = Visit<ExprNode>(context.expression());
      return new WhereNode(expr);
    }


    public override AstNode VisitReturnClause([NotNull] CypherParser.ReturnClauseContext context)
    {
      var items = new List<ReturnItemNode>();
      
      foreach (var itemCtx in context.returnItem())
      {
        items.Add(Visit<ReturnItemNode>(itemCtx));
      }
      
      return new ReturnNode(items);
    }


    public override AstNode VisitLimitClause([NotNull] global::CypherParser.LimitClauseContext context)
    {
      var value = (IntLiteralNode)Visit(context.integerLiteral());
      return new LimitNode((int)value.Value);
    }


    public override AstNode VisitReturnItem([NotNull] CypherParser.ReturnItemContext context)
    {
      var expr = Visit<ExprNode>(context.expression());
      var id = context.identifier() != null ? Visit<IdentifierNode>(context.identifier()) : null;
      return new ReturnItemNode(expr, id);
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


    public override AstNode VisitPatternChain([NotNull] CypherParser.PatternChainContext context)
    {
      var relPattern = Visit<RelationshipPatternNode>(context.relationshipPattern());
      var nodePattern = Visit<NodePatternNode>(context.nodePattern());
      return new PatternChainNode(relPattern, nodePattern);
    }


    public override AstNode VisitNodePattern([NotNull] CypherParser.NodePatternContext context)
    {
      var variable = context.variable() != null ? Visit<VariableNode>(context.variable()) : null;
      var label = context.nodeLabel() != null ? Visit<LabelNode>(context.nodeLabel()) : null;
      var propertyMap = context.propertyMap() != null ? Visit<PropertyMapNode>(context.propertyMap()) : null;
      var variableDef = NewVariableDefinition(variable, VariableKindType.Node);
      return new NodePatternNode(variableDef, label?.Label, propertyMap);
    }


    public override AstNode VisitRelationshipPattern([NotNull] CypherParser.RelationshipPatternContext context)
    {
      var detail = context.relationshipDetail() != null ? Visit<RelationshipDetailNode>(context.relationshipDetail()) : null;
      var direction = context.ARROW_RIGHT() != null
        ? RelationshipDirectionType.Right
        : context.ARROW_LEFT() != null
          ? RelationshipDirectionType.Left
          : RelationshipDirectionType.None;
      return new RelationshipPatternNode(detail, direction);
    }


    public override AstNode VisitRelationshipDetail([NotNull] CypherParser.RelationshipDetailContext context)
    {
      var variable = context.variable() != null ? Visit<VariableNode>(context.variable()) : null;
      var relationshipType = context.relationshipTypes() != null ? Visit<IdentifierNode>(context.relationshipTypes()) : null;
      var propertyMap = context.propertyMap() != null ? Visit<PropertyMapNode>(context.propertyMap()) : null;
      var variableDef = NewVariableDefinition(variable, VariableKindType.Node);
      return new RelationshipDetailNode(variableDef, relationshipType?.Name, propertyMap);
    }


    public override AstNode VisitRelationshipTypes([NotNull] CypherParser.RelationshipTypesContext context)
    {
      var id = Visit<IdentifierNode>(context.identifier());
      return id;
    }

    
    public override AstNode VisitNodeLabel([NotNull] CypherParser.NodeLabelContext context)
    {
      var label = context.identifier().GetText();
      return new LabelNode(label);
    }


    public override AstNode VisitPropertyMap([NotNull] CypherParser.PropertyMapContext context)
    {
      var properties = new List<PropertyEntryNode>();

      foreach (var p in context.propertyEntry())
      {
        var property = Visit<PropertyEntryNode>(p);
        properties.Add(property);
      }

      return new PropertyMapNode(properties);
    }


    public override AstNode VisitPropertyEntry([NotNull] CypherParser.PropertyEntryContext context)
    {
      var id = Visit<IdentifierNode>(context.identifier());
      var literalValue = Visit<LiteralValueNode>(context.literal());
      return new PropertyEntryNode(id.Name, literalValue);
    }

    #region Expressions

    public override AstNode VisitExpression([NotNull] CypherParser.ExpressionContext context)
    {
      return Visit<ExprNode>(context.orExpression());
    }


    public override AstNode VisitOrExpression([NotNull] CypherParser.OrExpressionContext context)
    {
      return VisitBinaryOperator(context.andExpression(), BinaryOperatorType.Or);
    }


    public override AstNode VisitAndExpression([NotNull] CypherParser.AndExpressionContext context)
    {
      return VisitBinaryOperator(context.notExpression(), BinaryOperatorType.And);
    }


    protected ExprNode VisitBinaryOperator(IEnumerable<IParseTree> context, BinaryOperatorType op)
    {
      var leftExpr = (ExprNode?)null;
      foreach (var exprCtx in context)
      {
        var expr = Visit<ExprNode>(exprCtx);
        if (leftExpr == null)
        {
          leftExpr = expr;
        }
        else
        {
          leftExpr = new BinaryOperatorNode(leftExpr, expr, op);
        }
      }
      return leftExpr!;
    }


    public override AstNode VisitNotExpression([NotNull] CypherParser.NotExpressionContext context)
    {
      if (context.notExpression() != null)
      {
        var expr = Visit<ExprNode>(context.notExpression());
        return new UnaryOperatorNode(UnaryOperatorType.Not, expr);
      }
      else
      {
        return Visit<ExprNode>(context.primaryExpression());
      }
    }


    public override AstNode VisitPrimaryExpression([NotNull] CypherParser.PrimaryExpressionContext context)
    {
      if (context.literal() != null)
      {
        return Visit<ExprNode>(context.literal());
      }
      else if (context.variable() != null)
      {
        var v = Visit<VariableNode>(context.variable());
        var vd = FindVariableDefinition(v, VariableKindType.Node);
        return new VariableExprNode(vd);
      }
      else if (context.expression() != null)
      {
        return Visit<ExprNode>(context.expression());
      }
      else if (context.propertyAccess() != null)
      {
        return Visit<PropertyAccessNode>(context.propertyAccess());
      }
      else
        throw new NotImplementedException();
    }


    public override AstNode VisitPropertyAccess([NotNull] CypherParser.PropertyAccessContext context)
    {
      var variable = Visit<VariableNode>(context.variable());
      var properties = new List<string>();
      foreach (var p in context.identifier())
      {
        var property = Visit<IdentifierNode>(p);
        properties.Add(property.Name);
      }
      return new PropertyAccessNode(FindVariableDefinition(variable, VariableKindType.Node), properties);
    }

    
    public override AstNode VisitVariable([NotNull] CypherParser.VariableContext context)
    {
      var name = context.identifier().GetText();
      return new VariableNode(name);
    }


    public override AstNode VisitIdentifier([NotNull] CypherParser.IdentifierContext context)
    {
      var id = context.IDENTIFIER().GetText();
      return new IdentifierNode(id);
    }

    
    #region Literals

    public override AstNode VisitLiteral([NotNull] CypherParser.LiteralContext context)
    {
      if (context.boolLiteral() != null)
      {
        return Visit<LiteralValueNode>(context.boolLiteral());
      }
      else if (context.integerLiteral() != null)
      {
        return Visit<LiteralValueNode>(context.integerLiteral());
      }
      else if (context.stringLiteral() != null)
      {
        return Visit<LiteralValueNode>(context.stringLiteral());
      }
      else
        throw new NotImplementedException();
    }


    public override AstNode VisitBoolLiteral([NotNull] CypherParser.BoolLiteralContext context)
    {
      return new BoolLiteralNode(context.TRUE() != null);
    }

    public override AstNode VisitIntegerLiteral([NotNull] CypherParser.IntegerLiteralContext context)
    {
      var text = context.INTEGER().GetText();
      if (!long.TryParse(text, out var value))
        throw new ArgumentException($"Not an integer: '{text}'.");
      return new IntLiteralNode(value);
    }


    public override AstNode VisitStringLiteral([NotNull] CypherParser.StringLiteralContext context)
    {
      var text = context.STRING().GetText();
      text = text.Substring(1, text.Length - 2);
      return new StringLiteralNode(text);
    }

    #endregion /* Literals */

    #endregion /* Expressions */

    #endregion


    #region Utilities

    public T Visit<T>(IParseTree tree) where T : AstNode
    {
      var result = base.Visit(tree);
      if (result is not T)
        throw new InvalidOperationException($"Unexpected type '{result?.GetType()}' - expected '{typeof(T)}'.");
      return (T)result;
    }

    
    private VariableDefinition NewVariableDefinition(VariableNode? variableNode, VariableKindType variableKind)
    {
      string variableName = variableNode?.Name ?? NewAnonymousVariable();

      if (VariableDefinitions.TryGetValue(variableName, out var variableDefinition))
      {
        throw new InvalidOperationException($"Variable '{variableName}' already defined.");
      }
      else
      {
        variableDefinition = new VariableDefinition(variableNode == null, variableName, variableKind, VariableDefinitions.Count);
        VariableDefinitions.Add(variableName, variableDefinition);
      }
      return variableDefinition;
    }



    private VariableDefinition FindVariableDefinition(VariableNode? variableNode, VariableKindType variableKind)
    {
      string variableName = variableNode?.Name ?? NewAnonymousVariable();

      if (VariableDefinitions.TryGetValue(variableName, out var variableDefinition))
      {
        if (variableDefinition.Kind != variableKind)
          throw new InvalidOperationException($"Variable '{variableName}' reused as '{variableDefinition.Kind}' - expected '{VariableKindType.Node}'.");
      }
      else
      {
        throw new InvalidOperationException($"Undefined variable '{variableName}'.");
      }
      return variableDefinition;
    }

    private string NewAnonymousVariable()
    {
      string name = "anon_" + AnonymousVariableCounter++;
      return name;
    }

    #endregion
  }
}
