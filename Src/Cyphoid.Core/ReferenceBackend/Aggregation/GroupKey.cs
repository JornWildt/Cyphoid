using System.Security.Cryptography;
using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.ReferenceBackend.Aggregation
{
  public sealed class GroupKey<TId> : IEquatable<GroupKey<TId>> where TId : IEquatable<TId>
  {
    public MixedValue?[] Values { get; }
    IReadOnlyList<GroupingEvaluator<TId>> Groupings;

    public GroupKey(
      IRow<TId> row,
      IReadOnlyList<GroupingEvaluator<TId>> groupings)
    {
      Groupings = groupings;
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


    public void WriteGroupData(IRow<TId> row)
    {
      for (int i = 0; i < Groupings.Count; i++)
      {
        var value = Values[i];
        row.Values[Groupings[i].OutputSlotIndex] = value;
      }
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
