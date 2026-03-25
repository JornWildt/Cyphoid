using System.Security.Cryptography;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.ReferenceBackend.Aggregation
{
  public sealed class GroupKey<TId> : IEquatable<GroupKey<TId>> where TId : IEquatable<TId>
  {
    public MixedValue?[] Values { get; }


    public GroupKey(
      IRow<TId> row,
      IReadOnlyList<GroupingEvaluator<TId>> groupings)
    {
      Values = new MixedValue?[groupings.Count];
      for (int i = 0; i < groupings.Count; i++)
      {
        Values[i] = groupings[i].Expression(row);
      }
    }


    bool IEquatable<GroupKey<TId>>.Equals(GroupKey<TId>? other)
    {
      if (other is null || other.Values.Length != Values.Length)
        return false;

      for (int i = 0; i < Values.Length; i++)
      {
        if (!Equals(Values[i], other.Values[i]))
          return false;
      }

      return true;
    }


    public override bool Equals(object? obj) => obj is GroupKey<TId> other && Equals(other);


    public override int GetHashCode()
    {
      var hash = new HashCode();
      foreach (var v in Values)
        hash.Add(v);
      return hash.ToHashCode();
    }
  }
}
