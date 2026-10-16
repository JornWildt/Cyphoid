using Cyphoid.Core.Execution;
using Cyphoid.Core.ReferenceBackend;

namespace Cyphoid.Tests.TestBackend
{
  internal class TestRow : Row
  {
    public ISet<string> NodeIdsForRow { get; private init; }


    public TestRow(ISet<string> nodeIds, RowSchema columns)
      : base(columns)
    {
      NodeIdsForRow = new HashSet<string>(nodeIds);
    }


    protected override Row CreateEmptyRow(RowSchema outputColumns)
    {
      return new TestRow(NodeIdsForRow, outputColumns);
    }


    protected override void CopyOtherContentToClone(IRow source, IRow clone)
    {
      base.CopyOtherContentToClone(source, clone);
      foreach (var id in ((TestRow)source).NodeIdsForRow)
        ((TestRow)clone).NodeIdsForRow.Add(id);
    }
  }
}
