namespace Cyphoid.Core.Execution
{
  public interface IRow
  {
    IReadOnlyList<IGraphNode> Nodes { get; }
    IRow Clone();
  }
}
