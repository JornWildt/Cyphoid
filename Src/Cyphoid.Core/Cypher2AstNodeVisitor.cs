using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Cyphoid.Core.Expressions;
using Cyphoid.Core.ReferenceBackend;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core
{
  public record VariableDefinition(bool IsAnonymous, string Name, MixedValue.ValueType Type, int SlotIndex);

  public enum VariableReferenceBindingType { Unbound, Bound }

  public record VariableReference(VariableReferenceBindingType Binding, VariableDefinition Definition);


  internal class Cypher2AstNodeVisitor : CypherBaseVisitor<AstNode>
  {
    int AnonymousVariableCounter = 1;

    Dictionary<string, VariableDefinition> VariableDefinitions = new Dictionary<string, VariableDefinition>();
    Dictionary<string, VariableDefinition> OutputVariableDefinitions = new Dictionary<string, VariableDefinition>();


    #region Visitors

    public override AstNode VisitQuery([NotNull] global::CypherParser.QueryContext context)
    {
      var clauses = new List<ClauseNode>();
      foreach (var clauseCtx in context.repeatableClause())
      {
        var c = Visit<ClauseNode>(clauseCtx);
        clauses.Add(c);
      }
      
      var returnLimit = Visit<ReturnLimitNode>(context.returnLimitClause());

      return new QueryNode(clauses, returnLimit);
    }


    public override AstNode VisitRepeatableClause([NotNull] CypherParser.RepeatableClauseContext context)
    {
      if (context.withClause() != null)
      {
        return Visit<WithNode>(context.withClause());
      }
      else if (context.matchWhereClause() != null)
      {
        return Visit<MatchWhereNode>(context.matchWhereClause());
      }
      else
        throw new NotImplementedException();
    }


    public override AstNode VisitWithClause([NotNull] CypherParser.WithClauseContext context)
    {
      // Projections dependents on current variable declarations and generates a new set of output variables
      var projections = Visit<ProjectionsNode>(context.projectionList());

      // Once the projections have been setup, the output variables becomes the new current variables.
      TransferOutputVariablesToCurrentVariables();

      return new WithNode(projections.Projections);
    }

    
    public override AstNode VisitMatchWhereClause([NotNull] CypherParser.MatchWhereClauseContext context)
    {
      var match = Visit<MatchNode>(context.matchClause());
      var where = context.whereClause() != null ? Visit<WhereNode>(context.whereClause()) : null;

      var declaredColumns = VariableDefinitions
        .Select((v, i) => new RowColumn(i, v.Key, v.Value.Type))
        .ToArray();

      return new MatchWhereNode(match.Pattern, where?.Expr, declaredColumns);
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


    public override AstNode VisitReturnLimitClause([NotNull] CypherParser.ReturnLimitClauseContext context)
    {
      var @return = Visit<ReturnNode>(context.returnClause());

      TransferOutputVariablesToCurrentVariables();

      var ordering = context.orderingClause() != null ? Visit<OrderByNode>(context.orderingClause()) : null;
      var limit = context.limitClause() != null ? Visit<LimitNode>(context.limitClause()) : null;
      return new ReturnLimitNode(@return.Projections.Projections, ordering, limit?.Limit);
    }


    public override AstNode VisitReturnClause([NotNull] CypherParser.ReturnClauseContext context)
    {
      var projections = Visit<ProjectionsNode>(context.projectionList());

      return new ReturnNode(projections);
    }


    public override AstNode VisitOrderingClause([NotNull] CypherParser.OrderingClauseContext context)
    {
      var item = Visit<OrderByItemNode>(context.orderByItem(0));
      var items = new List<OrderByItemNode>() { item };
      foreach (var itemCtx in context.orderByItem().Skip(1))
      {
        item = Visit<OrderByItemNode>(itemCtx);
        items.Add(item);
      }
      return new OrderByNode(items);
    }


    public override AstNode VisitOrderByItem([NotNull] CypherParser.OrderByItemContext context)
    {
      var expression = Visit<ExprNode>(context.expression());
      var dir = 
        context.ASC() != null ? OrderByDirectionType.Ascending
        : context.DESC() != null? OrderByDirectionType.Descending
        : OrderByDirectionType.DefaultAscending;
      return new OrderByItemNode(expression, dir);
    }


    public override AstNode VisitLimitClause([NotNull] global::CypherParser.LimitClauseContext context)
    {
      var value = (IntLiteralNode)Visit(context.integerLiteral());
      return new LimitNode((int)value.Value);
    }


    int AnonymousProjectionCounter = 1;

    public override AstNode VisitProjectionList([NotNull] CypherParser.ProjectionListContext context)
    {
      AnonymousProjectionCounter = 1;

      var projections = new List<ProjectionNode>();
      foreach (var projectionCtx in context.projectionItem())
      {
        var p = Visit<ProjectionNode>(projectionCtx);
        projections.Add(p);
      }
      return new ProjectionsNode(projections);
    }


    public override AstNode VisitProjectionItem([NotNull] CypherParser.ProjectionItemContext context)
    {
      var expr = Visit<ExprNode>(context.expression());
      var id = context.identifier() != null ? Visit<IdentifierNode>(context.identifier()) : null;

      var variableName = id?.Name ??
        ((expr is VariableExprNode v) ? v.Variable.Name
        : (expr is PropertyAccessNode pa) ? pa.Properties[pa.Properties.Count - 1]
        : $"p{AnonymousProjectionCounter++}");

      var variable = RegisterOutputVariable(id == null, variableName, MixedValue.ValueType.String);

      return new ProjectionNode(expr, variable);
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
      var nodeVariableRef = RegisterVariable(variable, MixedValue.ValueType.Node);
      return new NodePatternNode(nodeVariableRef, label?.Label, propertyMap);
    }


    public override AstNode VisitRelationshipPattern([NotNull] CypherParser.RelationshipPatternContext context)
    {
      var detail = context.relationshipDetail() != null ? Visit<RelationshipDetailNode>(context.relationshipDetail()) : null;
      var direction = context.ARROW_RIGHT() != null
        ? RelationshipDirectionType.Right
        : context.ARROW_LEFT() != null
          ? RelationshipDirectionType.Left
          : RelationshipDirectionType.Both;
      return new RelationshipPatternNode(detail, direction);
    }


    public override AstNode VisitRelationshipDetail([NotNull] CypherParser.RelationshipDetailContext context)
    {
      var variable = context.variable() != null ? Visit<VariableNode>(context.variable()) : null;
      var relationshipType = context.relationshipTypes() != null ? Visit<IdentifierNode>(context.relationshipTypes()) : null;
      var propertyMap = context.propertyMap() != null ? Visit<PropertyMapNode>(context.propertyMap()) : null;
      var edgeVariableRef = RegisterVariable(variable, MixedValue.ValueType.Edge);
      return new RelationshipDetailNode(edgeVariableRef, relationshipType?.Name, propertyMap);
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
        return Visit<ExprNode>(context.comparisonExpression());
      }
    }


    public override AstNode VisitComparisonExpression([NotNull] CypherParser.ComparisonExpressionContext context)
    {
      var left = Visit<ExprNode>(context.inExpression(0));
      if (context.inExpression(1) == null)
        return left;

      var operatorText = context.comparisonOperator().GetText().ToUpper();
      var op = operatorText switch
      {
        "=" => BinaryOperatorType.EQ,
        "<>" => BinaryOperatorType.NEQ,
        "<=" => BinaryOperatorType.LTE,
        ">=" => BinaryOperatorType.GTE,
        "<" => BinaryOperatorType.LT,
        ">" => BinaryOperatorType.GT,
        "CONTAINS" => BinaryOperatorType.CONTAINS,
        "STARTS WITH" => BinaryOperatorType.STARTS_WITH,
        "ENDS WITH" => BinaryOperatorType.ENDS_WITH,
        _ => throw new NotImplementedException()
      };

      var right = Visit<ExprNode>(context.inExpression(1));

      return new BinaryOperatorNode(left, right, op);
    }


    public override AstNode VisitInExpression([NotNull] CypherParser.InExpressionContext context)
    {
      var expr = Visit<ExprNode>(context.primaryExpression());
      if (context.IN() == null || context.expressionList() == null)
        return expr;

      var list = new List<ExprNode>();
      foreach (var e in context.expressionList().expression())
      {
        var expression = Visit<ExprNode>(e);
        list.Add(expression);
      }

      return new InOperatorNode(expr, list);
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
        var vd = FindVariable(v);
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
      else if (context.functionCall() != null)
      {
        return Visit<FunctionCallNode>(context.functionCall());
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
      return new PropertyAccessNode(FindVariable(variable), properties);
    }


    public override AstNode VisitFunctionCall([NotNull] CypherParser.FunctionCallContext context)
    {
      if (context.COUNT() != null && context.ASTERIX() != null)
        return new FunctionCallNode("CountAll", []);

      var name = context.identifier().GetText();
      var parameters = context.expression()
        .Select(e => Visit<ExprNode>(e))
        .ToArray();

      return new FunctionCallNode(name, parameters);
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
      else if (context.NULL() != null)
      {
        return new NullLiteralNode();
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


    private VariableReference RegisterVariable(VariableNode? variableNode, MixedValue.ValueType type)
    {
      string variableName = variableNode?.Name ?? NewAnonymousVariableName();

      return RegisterVariable(variableNode == null, variableName, type);
    }
    
    
    private VariableReference RegisterVariable(bool isAnonymous, string variableName, MixedValue.ValueType type)
    {
      if (VariableDefinitions.TryGetValue(variableName, out var variableDefinition))
      {
        return new VariableReference(VariableReferenceBindingType.Bound, variableDefinition);
      }
      else
      {
        int slotIndex = VariableDefinitions.Count;
        variableDefinition = new VariableDefinition(isAnonymous, variableName, type, slotIndex);
        VariableDefinitions.Add(variableName, variableDefinition);

        return new VariableReference(VariableReferenceBindingType.Unbound, variableDefinition);
      }
    }


    private VariableDefinition RegisterOutputVariable(bool isAnonymous, string variableName, MixedValue.ValueType type)
    {
      if (OutputVariableDefinitions.TryGetValue(variableName, out var variableDefinition))
      {
        throw new ($"Reuse of '{variableName}' in output list.");
      }
      else
      {
        int slotIndex = OutputVariableDefinitions.Count;
        variableDefinition = new VariableDefinition(isAnonymous, variableName, type, slotIndex);
        OutputVariableDefinitions.Add(variableName, variableDefinition);

        return variableDefinition;
      }
    }


    private VariableDefinition FindVariable(VariableNode variableNode)
    {
      string variableName = variableNode.Name;

      if (!VariableDefinitions.TryGetValue(variableName, out var variableDefinition))
      {
        throw new InvalidOperationException($"Undefined variable '{variableName}'.");
      }

      return variableDefinition;
    }

    private string NewAnonymousVariableName()
    {
      string name = "anon_" + AnonymousVariableCounter++;
      return name;
    }


    private void TransferOutputVariablesToCurrentVariables()
    {
      VariableDefinitions = new Dictionary<string, VariableDefinition>(OutputVariableDefinitions);
      OutputVariableDefinitions.Clear();
      AnonymousVariableCounter = 1;
    }

    #endregion
  }
}
