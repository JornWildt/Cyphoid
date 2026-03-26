using System.Text;

namespace Cyphoid.Core.SyntaxTree
{
  public record ProjectionsNode(
    IReadOnlyList<ProjectionNode> Projections) : AstNode
  {
  }


  public record ProjectionNode(ExprNode Expr, VariableDefinition Variable) : AstNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      Expr.PrettyPrint(sb);
      if (!Variable.IsAnonymous)
      {
        sb.Append(" AS ");
        sb.Append(Variable.Name);
      }
    }
  }
}
