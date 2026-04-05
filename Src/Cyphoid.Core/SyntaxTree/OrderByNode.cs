using System.Text;

namespace Cyphoid.Core.SyntaxTree
{
  public enum OrderByDirectionType { DefaultAscending, Ascending, Descending }


  public record OrderByNode(
    IReadOnlyList<OrderByItemNode> Ordering) : AstNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("ORDER BY ");
      bool first = true;
      foreach (var item in Ordering)
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

#pragma warning disable CS8524 // unnamed enum values
      var dir = Direction switch
      {
        OrderByDirectionType.DefaultAscending => "",
        OrderByDirectionType.Ascending => " ASC",
        OrderByDirectionType.Descending => " DESC",
      };
      sb.Append(dir);
#pragma warning restore CS8524
    }
  }
}
