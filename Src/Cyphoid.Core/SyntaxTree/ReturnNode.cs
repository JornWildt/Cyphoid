using System.Text;

namespace Cyphoid.Core.SyntaxTree
{
  public record ReturnNode(IReadOnlyList<ReturnProjectionNode> Projections) : AstNode;


    public record ReturnProjectionNode(ExprNode Expr, VariableDefinition Variable) : AstNode
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
