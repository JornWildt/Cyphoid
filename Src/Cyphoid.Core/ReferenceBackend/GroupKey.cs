using Cyphoid.Core.Execution;
using Cyphoid.Core.Expressions;

namespace Cyphoid.Core.ReferenceBackend
{
  public sealed class GroupKey : IEquatable<GroupKey>
  {
    public MixedValue?[] Values { get; }
    IReadOnlyList<GroupingEvaluator> Groupings;

    public GroupKey(
      IRow row,
      IReadOnlyList<GroupingEvaluator> groupings)
    {
      Groupings = groupings;
      Values = new MixedValue?[groupings.Count];
      for (int i = 0; i < groupings.Count; i++)
      {
        Values[i] = groupings[i].Expression(row);
      }
    }


    bool IEquatable<GroupKey>.Equals(GroupKey? other)
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


    public void WriteGroupData(IRow row)
    {
      for (int i = 0; i < Groupings.Count; i++)
      {
        var value = Values[i];
        row.Values[Groupings[i].OutputSlotIndex] = value;
      }
    }

    public override bool Equals(object? obj) => obj is GroupKey other && Equals(other);


    public override int GetHashCode()
    {
      var hash = new HashCode();
      foreach (var v in Values)
        hash.Add(v);
      return hash.ToHashCode();
    }
  }
}
