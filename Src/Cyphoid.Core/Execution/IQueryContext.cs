namespace Cyphoid.Core.Execution
{
  public interface IQueryContext
  {
    IQueryRowFactory RowFactory { get; }
  }


  public interface IQueryRowFactory
  {
    // Create clean empty row.
    IRow EmptyRow(IReadOnlyList<IRowColumn> columns);

    // Create empty row with optional implementation specific meta data from a source row.
    IRow EmptyRow(IRow source, IReadOnlyList<IRowColumn> columns);
  }
}
