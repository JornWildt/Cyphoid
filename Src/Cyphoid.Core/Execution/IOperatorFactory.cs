using Cyphoid.Core.SyntaxTree;

namespace Cyphoid.Core.Execution
{
  public interface IOperatorFactory
  {
    IOperator BuildProjection(IOperator input);
    
    IOperator BuildNodeScan(
      VariableDefinition variable,
      string? label,
      PropertyMapNode? propertyMap);
  }
}
