namespace Cyphoid.Core.SyntaxTree
{
  public record ReturnNode(bool Distinct, ProjectionsNode Projections) : AstNode;
}
