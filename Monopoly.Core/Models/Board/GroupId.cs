using Monopoly.Core.Presentation;

namespace Monopoly.Core.Models.Board;

/// <summary>An authoritative property-group identity that is independent of presentation.</summary>
public readonly struct GroupId : IEquatable<GroupId>, IComparable<GroupId>
{
    public GroupId(string value)
    {
        PresentationToken validated = new(value);
        Value = validated.Value;
    }

    public string Value { get; }
    public bool IsValid => Value is not null && new PresentationToken(Value).IsValid;

    public int CompareTo(GroupId other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public bool Equals(GroupId other) => StringComparer.Ordinal.Equals(Value, other.Value);
    public override bool Equals(object? obj) => obj is GroupId other && Equals(other);
    public override int GetHashCode() => Value is null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value ?? string.Empty;
    public static bool operator ==(GroupId left, GroupId right) => left.Equals(right);
    public static bool operator !=(GroupId left, GroupId right) => !left.Equals(right);
}
