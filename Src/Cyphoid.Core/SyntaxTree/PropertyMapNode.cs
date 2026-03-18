using System.Text;

namespace Cyphoid.Core.SyntaxTree
{
  public record PropertyMapNode(IReadOnlyList<PropertyEntryNode> Properties) : AstNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("{");
      bool first = true;
      foreach (var property in Properties)
      {
        if (!first)
          sb.Append(", ");
        property.PrettyPrint(sb);
        first = false;
      }
      sb.Append("}");
    }
  }


  public record PropertyEntryNode(string Identifier, LiteralValueNode Value) : AstNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append(Identifier + ": ");
      Value.PrettyPrint(sb);
    }
  }
}
