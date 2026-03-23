using System.Text;

namespace Cyphoid.Core.SyntaxTree
{
  public enum OrderByDirectionType { DefaultAscending, Ascending, Descending }


  public record OrderByNode(
    IReadOnlyList<OrderByItemNode> Items) : AstNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("ORDER BY ");
      bool first = true;
      foreach (var item in Items)
      {
        if (!first)
          sb.Append(", ");
        item.PrettyPrint(sb);
        first = false;
      }
    }
  }


  public record OrderByItemNode(
    ExprNode Expression,
    OrderByDirectionType Direction) : AstNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      Expression.PrettyPrint(sb);
      var dir = Direction switch
      {
        OrderByDirectionType.DefaultAscending => "",
        OrderByDirectionType.Ascending => " ASC",
        OrderByDirectionType.Descending => " DESC",
        _ => throw new NotImplementedException()
      };
      sb.Append(dir);
    }
  }
}
