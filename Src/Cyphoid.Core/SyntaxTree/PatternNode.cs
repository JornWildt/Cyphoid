using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Planning;

namespace Cyphoid.Core.SyntaxTree
{
  public record PatternNode(IReadOnlyList<PatternPartNode> Parts) : AstNode
  {
    public PipelinePlan BuildPlan(
      PipelinePlan? input,
      IRowColumn[] matchColumns)
    {
      foreach (var part in Parts)
        input = BuildPartPlan(part, input, matchColumns);

      return input ?? new EmptyPlan();
    }

    private static PipelinePlan BuildPartPlan(
      PatternPartNode part,
      PipelinePlan? input,
      IRowColumn[] matchColumns)
    {
      // A two-node pattern can start at its bound right endpoint without scanning the left node.
      if (part.PatternChain.Count == 2 &&
          part.PatternChain[0].NodePattern.Variable.Binding == VariableReferenceBindingType.Unbound &&
          part.PatternChain[1].NodePattern.Variable.Binding == VariableReferenceBindingType.Bound &&
          part.PatternChain[0].NodePattern.Variable.Definition.SlotIndex !=
            part.PatternChain[1].NodePattern.Variable.Definition.SlotIndex)
      {
        var relationship = part.PatternChain[1].RelationshipPattern!;
        var reversed = relationship with
        {
          RelationshipDirection = relationship.RelationshipDirection switch
          {
            RelationshipDirectionType.Right => RelationshipDirectionType.Left,
            RelationshipDirectionType.Left => RelationshipDirectionType.Right,
            _ => relationship.RelationshipDirection
          }
        };
        var reversedPart = new PatternPartNode([
          new PatternChainNode(null, part.PatternChain[1].NodePattern),
          new PatternChainNode(reversed, part.PatternChain[0].NodePattern)]);
        return BuildPartPlan(reversedPart, input, matchColumns);
      }

      // Parse guarantees existence of initial (left most) node.
      var initialNodePattern = part.PatternChain[0].NodePattern;

      var nextRelationshipPattern = part.PatternChain.Count > 1
        ? part.PatternChain[1].RelationshipPattern
        : null;

      PipelinePlan plan;
      if (initialNodePattern.Variable.Binding == VariableReferenceBindingType.Unbound)
      {
        plan = new NodeScanPlan(
          initialNodePattern.Variable,
          initialNodePattern.Label,
          initialNodePattern.PropertyMap,
          nextRelationshipPattern,
          matchColumns);

        if (input != null)
        {
          plan = new CartesianProductPlan(
            input,
            plan);
        }
      }
      else
      {
        ArgumentNullException.ThrowIfNull(input, nameof(input));

        plan = new ExpandIntoPlan(
          input,
          null,
          null,
          initialNodePattern.Variable,
          initialNodePattern.Label,
          initialNodePattern.PropertyMap);
      }

      var previousChainNode = part.PatternChain[0];
      foreach (var chainNode in part.PatternChain.Skip(1))
      {
        if (chainNode.NodePattern.Variable.Binding == VariableReferenceBindingType.Unbound)
        {
          plan = new ExpandAllPlan(
            plan,
            previousChainNode.NodePattern.Variable,
            chainNode.RelationshipPattern!,
            chainNode.NodePattern.Variable,
            chainNode.NodePattern.Label,
            chainNode.NodePattern.PropertyMap,
            matchColumns);
        }
        else
        {
          plan = new ExpandIntoPlan(
            plan,
            previousChainNode.NodePattern.Variable,
            chainNode.RelationshipPattern!,
            chainNode.NodePattern.Variable,
            chainNode.NodePattern.Label,
            chainNode.NodePattern.PropertyMap);
        }
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

  public enum RelationshipDirectionType { Right, Left, Unidirectional }

  public record RelationshipPatternNode(RelationshipDetailNode? RelationshipDetail, RelationshipDirectionType RelationshipDirection) : AstNode
  {
    public override void PrettyPrint(StringBuilder sb)
    {
      if (RelationshipDirection == RelationshipDirectionType.Right || RelationshipDirection == RelationshipDirectionType.Unidirectional)
        sb.Append("-[");
      else if (RelationshipDirection == RelationshipDirectionType.Left)
        sb.Append("<-[");

      if (RelationshipDetail != null)
        RelationshipDetail.PrettyPrint(sb);

      if (RelationshipDirection == RelationshipDirectionType.Left || RelationshipDirection == RelationshipDirectionType.Unidirectional)
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
