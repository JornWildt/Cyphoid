using System.Text;
using Cyphoid.Core.Execution;
using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Planning
{
  public record ProjectionPlan(
    PipelinePlan Input,
    IReadOnlyList<ReturnItemNode> Projections) : LogicalPlan
  {
    public IProjectionOperator BuildExecutionPlan(IOperatorFactory factory)
    {
      int num = 1;
      var projections = new List<ProjectionEvaluator>();
      foreach (var p in Projections)
      {
        var name = p.Identifier?.Name ??
          ((p.Expr is VariableExprNode v) ? v.Variable.Name 
          : (p.Expr is PropertyAccessNode pa) ? pa.Properties[pa.Properties.Count-1]
          : $"p{num++}");
        var evaluator = p.Expr.BuildEvaluator();
        projections.Add(new ProjectionEvaluator(evaluator, name));
      }
      
      return factory.BuildProjection(
        Input.BuildExecutionPlan(factory), 
        projections);
    }

    
    public override void PrettyPrint(StringBuilder sb)
    {
      sb.Append("Project: ");
      bool first = true;
      foreach (var i in Projections)
      {
        if (!first)
          sb.Append(", ");
        i.Expr.PrettyPrint(sb);
        if (i.Identifier != null)
          sb.Append(" AS " + i.Identifier);
      }
      sb.AppendLine();
      Input.PrettyPrint(sb);
    }
  }
}
