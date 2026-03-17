using System.Text;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record PatternNode(IReadOnlyList<PatternPartNode> Parts) : AstNode
  {
    public LogicalPlan BuildPlan()
    {
      // FIXME: Handle multiple parts (assuming only and always one)

      var initialNodePattern = Parts[0].PatternChain[0].NodePattern;

      var variable = initialNodePattern.Variable != null ? initialNodePattern.Variable : null;
      var rootPlan = new NodeScanPlan(
        variable != null ? new NodeVariable(variable.Name, variable.SlotIndex) : null,
        initialNodePattern.Label,
        initialNodePattern.PropertyMap);

      return rootPlan;
    }


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

  public record NodePatternNode(VariableDefinition? Variable, string? Label, PropertyMapNode? PropertyMap) : AstNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("(");
      if (Variable != null)
        sb.Append(Variable.Name);
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

  public record RelationshipDetailNode(VariableDefinition? Variable, string? RelationshipType, PropertyMapNode? PropertyMap) : AstNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      if (Variable != null)
        sb.Append(Variable.Name);
      if (RelationshipType != null)
        sb.Append(":" + RelationshipType);
      if (PropertyMap != null)
      {
        sb.Append(" ");
        PropertyMap.PrettyPrint(sb);
      }
    }
  }
}
