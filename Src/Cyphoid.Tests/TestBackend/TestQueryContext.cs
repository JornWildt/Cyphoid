using Cyphoid.Core.Execution;

namespace Cyphoid.Tests.TestBackend
{
  internal class TestQueryContext : IQueryContext
  {
    RowFactory RowFactory = new RowFactory();

    IQueryRowFactory IQueryContext.RowFactory => RowFactory;
  }


  internal class RowFactory : IQueryRowFactory
  {
    IRow IQueryRowFactory.EmptyRow(RowSchema columns) => new TestRow(new HashSet<string>(), columns);


    IRow IQueryRowFactory.EmptyRow(IRow source, RowSchema columns)
    {
      var sourceRow = (TestRow)source;
      return new TestRow(sourceRow.NodeIdsForRow, columns);
    }
  }
}
