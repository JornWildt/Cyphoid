namespace Cyphoid.Core.Execution
{
  public interface IQueryContext
  {
    IQueryRowFactory RowFactory { get; }
  }


  public interface IQueryRowFactory
  {
    // Create a clean row retaining this exact immutable schema instance.
    IRow EmptyRow(RowSchema columns);

    // Preserve host metadata from source and retain the supplied schema instance.
    IRow EmptyRow(IRow source, RowSchema columns);
  }
}
