using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.ReferenceBackend
{
  public record UnionReferenceOperator(
    IReadOnlyList<IOperator> Branches, 
    bool All) : IOperator
  {
    public async IAsyncEnumerable<IRow> ExecuteAsync(IQueryContext context)
    {
      var seen = All ? null : new HashSet<MixedValue[]>(new RowValuesComparer());
      foreach (var branch in Branches)
        await foreach (var row in branch.ExecuteAsync(context))
        {
          if (seen != null && !seen.Add(row.Values.Select(v => v ?? MixedValue.Null()).ToArray()))
            continue;
          yield return row;
        }
    }

    private sealed class RowValuesComparer : IEqualityComparer<MixedValue[]>
    {
      public bool Equals(MixedValue[]? left, MixedValue[]? right)
        => ReferenceEquals(left, right) || (left != null && right != null && left.SequenceEqual(right));

      public int GetHashCode(MixedValue[] values)
      {
        var hash = new HashCode();
        foreach (var value in values)
          // Node equality is backend-defined; object identity hashes cannot be used here.
          hash.Add(value.Type == MixedValue.ValueType.Node ? 0 : value.GetHashCode());
        return hash.ToHashCode();
      }
    }
  }
}
