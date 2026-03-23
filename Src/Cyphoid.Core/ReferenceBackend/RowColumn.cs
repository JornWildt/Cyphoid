using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.ReferenceBackend
{
  public record RowColumn(int Index, string Name, MixedValue.ValueType Type) : IRowColumn;
}
