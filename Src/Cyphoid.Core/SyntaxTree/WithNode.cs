namespace Cyphoid.Core.SyntaxTree
{
  public record WithNode(IReadOnlyList<ProjectionNode> Projections) : AstNode;
}
