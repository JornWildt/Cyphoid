using System.Text;

namespace Cyphoid.Core.SyntaxTree
{
  public record ReturnNode(ProjectionsNode Projections) : AstNode;
}
