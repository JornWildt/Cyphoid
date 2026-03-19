namespace Cyphoid.Core.Execution
{
  public interface IRow
  {
    IGraphNode?[] Nodes { get; }
    IRow Clone();
  }
}
