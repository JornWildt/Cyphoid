using System.Text;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record PatternNode(IReadOnlyList<PatternPartNode> Parts) : AstNode
  {
    public PipelinePlan<TId> BuildPlan<TId>() where TId : IEquatable<TId>
    {
      // FIXME: Only using first part so far
      var part = Parts[0];

      // Parse guarantees existence of initial (left most) node.
      var initialNodePattern = part.PatternChain[0].NodePattern;

      PipelinePlan<TId> plan = new NodeScanPlan<TId>(
        initialNodePattern.Variable,
        initialNodePattern.Label,
        initialNodePattern.PropertyMap);

      var previousChainNode = part.PatternChain[0];
      foreach (var chainNode in part.PatternChain.Skip(1))
      {
        plan = new ExpandPlan<TId>(
          plan,
          previousChainNode.NodePattern.Variable,
          chainNode.RelationshipPattern!,
          chainNode.NodePattern.Variable,
          chainNode.NodePattern.Label,
          chainNode.NodePattern.PropertyMap);
        previousChainNode = chainNode;
      }

      return plan;
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

  public record NodePatternNode(VariableReference Variable, string? Label, PropertyMapNode? PropertyMap) : AstNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("(");
      if (!Variable.Definition.IsAnonymous)
        sb.Append(Variable.Definition.Name);
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

  public enum RelationshipDirectionType { Right, Left, Both }

  public record RelationshipPatternNode(RelationshipDetailNode? RelationshipDetail, RelationshipDirectionType RelationshipDirection) : AstNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      if (RelationshipDirection == RelationshipDirectionType.Right || RelationshipDirection == RelationshipDirectionType.Both)
        sb.Append("-[");
      else if (RelationshipDirection == RelationshipDirectionType.Left)
        sb.Append("<-[");

      if (RelationshipDetail != null)
        RelationshipDetail.PrettyPrint(sb);

      if (RelationshipDirection == RelationshipDirectionType.Left || RelationshipDirection == RelationshipDirectionType.Both)
        sb.Append("]-");
      else if (RelationshipDirection == RelationshipDirectionType.Right)
        sb.Append("]->");
    }
  }

  public record RelationshipDetailNode(
    VariableReference Variable, 
    string? RelationshipType, 
    PropertyMapNode? PropertyMap) : AstNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      if (!Variable.Definition.IsAnonymous)
        sb.Append(Variable.Definition.Name);
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
