using System.Text;

namespace Cyphoid.Core.SyntaxTree
{
  public abstract record AstNode
  {
    public virtual string PrettyPrint()
    {
      StringBuilder sb = new StringBuilder();
      PrettyPrint(sb);
      return sb.ToString();
    }
    
    public virtual void PrettyPrint(StringBuilder sb) { sb.Append($"-not-implemented({GetType().Name})-"); }
  }

  public record QueryNode(MatchNode Match, WhereNode? Where, ReturnNode Return, LimitNode? Limit) : AstNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      Match.PrettyPrint(sb);
      if (Where != null)
      {
        sb.Append(" ");
        Where.PrettyPrint(sb);
      }
      sb.Append(" ");
      Return.PrettyPrint(sb);
      if (Limit != null)
      {
        sb.Append(" ");
        Limit.PrettyPrint(sb);
      }
    }
  }

  
  public record MatchNode(PatternNode Pattern) : AstNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("MATCH ");
      Pattern.PrettyPrint(sb);
    }
  }

  
  public record WhereNode(ExprNode Expr) : AstNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("WHERE ");
      Expr.PrettyPrint(sb);
    }
  }

  
  public record ReturnNode(IReadOnlyList<ReturnItemNode> Items) : AstNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("RETURN ");
      bool first = true;
      foreach (var i in Items)
      {
        if (!first)
          sb.Append(", ");
        i.PrettyPrint(sb);
        first = false;
      }
    }
  }

  
  public record LimitNode(long Limit) : AstNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("LIMIT " + Limit);
    }
  }

  
  public record ReturnItemNode(ExprNode Expr, IdentifierNode? Identifier) : AstNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      Expr.PrettyPrint(sb);
      if (Identifier != null)
      {
        sb.Append(" AS ");
        Identifier.PrettyPrint(sb);
      }
    }
  }

  
  public record PatternNode(IReadOnlyList<PatternPartNode> Parts) : AstNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      bool first = true;
      foreach (var part in Parts)
      {
        if (!first)
          sb.Append(", ");
        part.PrettyPrint(sb);
        first = false;
      }
    }
  }

  // First Pattern is root pattern with no relationship (null).
  public record PatternPartNode(IReadOnlyList<PatternChainNode> PatternChain) : AstNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      foreach (var p in PatternChain)
      {
        p.PrettyPrint(sb);
      }
    }
  }

  public record PatternChainNode(RelationshipPatternNode? RelationshipPattern, NodePatternNode NodePattern) : AstNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      if (RelationshipPattern != null)
        RelationshipPattern.PrettyPrint(sb);
      NodePattern.PrettyPrint(sb);
    }
  }

  public record NodePatternNode(string? Variable, string? Label, PropertyMapNode? PropertyMap) : AstNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("(");
      if (Variable != null)
        sb.Append(Variable);
      if (Label != null)
        sb.Append(":" + Label);
      if (PropertyMap != null)
      {
        sb.Append(" ");
        PropertyMap.PrettyPrint(sb);
      }
      sb.Append(")");
    }
  }

  public enum RelationshipDirectionType { Right, Left, None }

  public record RelationshipPatternNode(RelationshipDetailNode? RelationshipDetail, RelationshipDirectionType RelationshipDirection) : AstNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      if (RelationshipDirection == RelationshipDirectionType.Right || RelationshipDirection == RelationshipDirectionType.None)
        sb.Append("-[");
      else if (RelationshipDirection == RelationshipDirectionType.Left)
        sb.Append("<-[");

      if (RelationshipDetail != null)
        RelationshipDetail.PrettyPrint(sb);
      
      if (RelationshipDirection == RelationshipDirectionType.Left || RelationshipDirection == RelationshipDirectionType.None)
        sb.Append("]-");
      else if (RelationshipDirection == RelationshipDirectionType.Right)
        sb.Append("]->");
    }
  }

  public record RelationshipDetailNode(string? Variable, string? RelationshipType, PropertyMapNode? PropertyMap) : AstNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      if (Variable != null)
        sb.Append(Variable);
      if (RelationshipType != null)
        sb.Append(":" + RelationshipType);
      if (PropertyMap != null)
      {
        sb.Append(" ");
        PropertyMap.PrettyPrint(sb);
      }
    }
  }


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

  
  public record PropertyEntryNode(string Identifier, ExprNode Expr) : AstNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append(Identifier + ": ");
      Expr.PrettyPrint(sb);
    }
  }

  
  
  public record VariableNode(string Name) : AstNode;

  public record LabelNode(string Label) : AstNode;

  
  public abstract record ExprNode : AstNode;

  
  public enum BinaryOperatorType { And, Or }

  public record BinaryOperatorNode(ExprNode Left, ExprNode Right, BinaryOperatorType Operator) : ExprNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      Left.PrettyPrint(sb);
      sb.Append(" " + Operator.ToString().ToUpper() + " ");
      Right.PrettyPrint(sb);
    }
  }


  public enum UnaryOperatorType { Not }

  public record UnaryOperatorNode(UnaryOperatorType Operator, ExprNode Expr) : ExprNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("NOT ");
      Expr.PrettyPrint(sb);
    }
  }


  public record IntLiteralNode(long Value) : ExprNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append(Value);
    }
  }


  public record StringLiteralNode(string Value) : ExprNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("\"" +  Value + "\"");
    }
  }

  
  public record IdentifierNode(string Name) : ExprNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append(Name);
    }
  }
}
