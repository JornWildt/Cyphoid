namespace Cyphoid.Core.SyntaxTree
{
  public abstract record AstNode;

  public record QueryNode(MatchNode Match, WhereNode? Where, ReturnNode Return, LimitNode? Limit) : AstNode;

  public record MatchNode(PatternNode Pattern) : AstNode;

  public record WhereNode(ExprNode Expr) : AstNode;

  public record ReturnNode(IReadOnlyList<ReturnItemNode> Items) : AstNode;

  public record LimitNode(int Limit) : AstNode;

  public record ReturnItemNode(ExprNode Expr, IdentifierNode? Identifier) : AstNode;

  public record PatternNode(IReadOnlyList<PatternPartNode> Parts) : AstNode;

  // First Pattern is root pattern with no relationship (null).
  public record PatternPartNode(IReadOnlyList<PatternChainNode> PatternChain) : AstNode;

  public record PatternChainNode(RelationshipPatternNode? RelationshipPattern, NodePatternNode NodePattern) : AstNode;

  public record NodePatternNode(string? Variable, string? Label, PropertyMapNode? PropertyMap) : AstNode;

  public enum RelationshipDirectionType { Right, Left, None }

  public record RelationshipPatternNode(RelationshipDetailNode RelationshipDetail, RelationshipDirectionType RelationshipDirection) : AstNode;

  public record RelationshipDetailNode(string? Variable, string? RelationshipType, PropertyMapNode PropertyMap);

  public record PropertyMapNode(IReadOnlyList<PropertyEntryNode> Properties);

  public record PropertyEntryNode(string Identifier, ExprNode Expr);

  public abstract record ExprNode : AstNode;

  public record IntLiteralNode(int Value) : ExprNode;

  public record StringLiteralNode(string Value) : ExprNode;

  public record IdentifierNode(string Name) : ExprNode;
}
