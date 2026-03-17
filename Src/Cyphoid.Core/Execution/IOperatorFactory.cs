namespace Cyphoid.Core.Execution
{
  public interface IOperatorFactory
  {
    IOperator BuildProjection(IOperator input);
    IOperator BuildNodeScan();
  }
}
