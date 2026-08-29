using System.Text.RegularExpressions;

namespace Monopoly.Core;

public readonly struct ProfileId : IEquatable<ProfileId>, IComparable<ProfileId>
{
    public const int MaximumLength = StructuralIdentifier.MaximumLength;

    public ProfileId(string value) => Value = StructuralIdentifier.Validate(value, nameof(value));

    public string Value { get; }
    public bool IsValid => StructuralIdentifier.IsValid(Value);
    public int CompareTo(ProfileId other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public bool Equals(ProfileId other) => StringComparer.Ordinal.Equals(Value, other.Value);
    public override bool Equals(object? obj) => obj is ProfileId other && Equals(other);
    public override int GetHashCode() => Value is null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value ?? string.Empty;
    public static bool operator ==(ProfileId left, ProfileId right) => left.Equals(right);
    public static bool operator !=(ProfileId left, ProfileId right) => !left.Equals(right);
}

public readonly struct ProfileRevision : IEquatable<ProfileRevision>, IComparable<ProfileRevision>
{
    public ProfileRevision(int value)
    {
        if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "A profile revision must be positive.");
        Value = value;
    }

    public int Value { get; }
    public bool IsValid => Value > 0;
    public int CompareTo(ProfileRevision other) => Value.CompareTo(other.Value);
    public bool Equals(ProfileRevision other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is ProfileRevision other && Equals(other);
    public override int GetHashCode() => Value;
    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    public static bool operator ==(ProfileRevision left, ProfileRevision right) => left.Equals(right);
    public static bool operator !=(ProfileRevision left, ProfileRevision right) => !left.Equals(right);
}

public readonly struct ProfileFingerprint : IEquatable<ProfileFingerprint>, IComparable<ProfileFingerprint>
{
    public const int HexLength = 64;
    private static readonly Regex CanonicalSha256 = new(
        "^[0-9a-f]{64}$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    public ProfileFingerprint(string value)
    {
        if (string.IsNullOrEmpty(value) || !CanonicalSha256.IsMatch(value))
            throw new ArgumentException("A profile fingerprint must be exactly 64 lowercase SHA-256 hexadecimal characters.", nameof(value));
        Value = value;
    }

    public string Value { get; }
    public bool IsValid => Value is not null && CanonicalSha256.IsMatch(Value);
    public int CompareTo(ProfileFingerprint other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public bool Equals(ProfileFingerprint other) => StringComparer.Ordinal.Equals(Value, other.Value);
    public override bool Equals(object? obj) => obj is ProfileFingerprint other && Equals(other);
    public override int GetHashCode() => Value is null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value ?? string.Empty;
    public static bool operator ==(ProfileFingerprint left, ProfileFingerprint right) => left.Equals(right);
    public static bool operator !=(ProfileFingerprint left, ProfileFingerprint right) => !left.Equals(right);
}

public readonly struct CapabilityId : IEquatable<CapabilityId>, IComparable<CapabilityId>
{
    public CapabilityId(string value) => Value = StructuralIdentifier.Validate(value, nameof(value));
    public string Value { get; }
    public bool IsValid => StructuralIdentifier.IsValid(Value);
    public int CompareTo(CapabilityId other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public bool Equals(CapabilityId other) => StringComparer.Ordinal.Equals(Value, other.Value);
    public override bool Equals(object? obj) => obj is CapabilityId other && Equals(other);
    public override int GetHashCode() => Value is null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value ?? string.Empty;
    public static bool operator ==(CapabilityId left, CapabilityId right) => left.Equals(right);
    public static bool operator !=(CapabilityId left, CapabilityId right) => !left.Equals(right);
}

public readonly struct EffectKindId : IEquatable<EffectKindId>, IComparable<EffectKindId>
{
    public EffectKindId(string value) => Value = StructuralIdentifier.Validate(value, nameof(value));
    public string Value { get; }
    public bool IsValid => StructuralIdentifier.IsValid(Value);
    public int CompareTo(EffectKindId other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public bool Equals(EffectKindId other) => StringComparer.Ordinal.Equals(Value, other.Value);
    public override bool Equals(object? obj) => obj is EffectKindId other && Equals(other);
    public override int GetHashCode() => Value is null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value ?? string.Empty;
    public static bool operator ==(EffectKindId left, EffectKindId right) => left.Equals(right);
    public static bool operator !=(EffectKindId left, EffectKindId right) => !left.Equals(right);
}

public readonly struct ResourceId : IEquatable<ResourceId>, IComparable<ResourceId>
{
    public ResourceId(string value) => Value = StructuralIdentifier.Validate(value, nameof(value));
    public string Value { get; }
    public bool IsValid => StructuralIdentifier.IsValid(Value);
    public int CompareTo(ResourceId other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public bool Equals(ResourceId other) => StringComparer.Ordinal.Equals(Value, other.Value);
    public override bool Equals(object? obj) => obj is ResourceId other && Equals(other);
    public override int GetHashCode() => Value is null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value ?? string.Empty;
    public static bool operator ==(ResourceId left, ResourceId right) => left.Equals(right);
    public static bool operator !=(ResourceId left, ResourceId right) => !left.Equals(right);
}

public readonly struct StatusId : IEquatable<StatusId>, IComparable<StatusId>
{
    public StatusId(string value) => Value = StructuralIdentifier.Validate(value, nameof(value));
    public string Value { get; }
    public bool IsValid => StructuralIdentifier.IsValid(Value);
    public int CompareTo(StatusId other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public bool Equals(StatusId other) => StringComparer.Ordinal.Equals(Value, other.Value);
    public override bool Equals(object? obj) => obj is StatusId other && Equals(other);
    public override int GetHashCode() => Value is null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value ?? string.Empty;
    public static bool operator ==(StatusId left, StatusId right) => left.Equals(right);
    public static bool operator !=(StatusId left, StatusId right) => !left.Equals(right);
}

public readonly struct DecisionKindId : IEquatable<DecisionKindId>, IComparable<DecisionKindId>
{
    public DecisionKindId(string value) => Value = StructuralIdentifier.Validate(value, nameof(value));
    public string Value { get; }
    public bool IsValid => StructuralIdentifier.IsValid(Value);
    public int CompareTo(DecisionKindId other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public bool Equals(DecisionKindId other) => StringComparer.Ordinal.Equals(Value, other.Value);
    public override bool Equals(object? obj) => obj is DecisionKindId other && Equals(other);
    public override int GetHashCode() => Value is null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value ?? string.Empty;
    public static bool operator ==(DecisionKindId left, DecisionKindId right) => left.Equals(right);
    public static bool operator !=(DecisionKindId left, DecisionKindId right) => !left.Equals(right);
}

public readonly struct DecisionOptionId : IEquatable<DecisionOptionId>, IComparable<DecisionOptionId>
{
    public DecisionOptionId(string value) => Value = StructuralIdentifier.Validate(value, nameof(value));
    public string Value { get; }
    public bool IsValid => StructuralIdentifier.IsValid(Value);
    public int CompareTo(DecisionOptionId other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public bool Equals(DecisionOptionId other) => StringComparer.Ordinal.Equals(Value, other.Value);
    public override bool Equals(object? obj) => obj is DecisionOptionId other && Equals(other);
    public override int GetHashCode() => Value is null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value ?? string.Empty;
    public static bool operator ==(DecisionOptionId left, DecisionOptionId right) => left.Equals(right);
    public static bool operator !=(DecisionOptionId left, DecisionOptionId right) => !left.Equals(right);
}

public readonly record struct ResourceAmount
{
    public ResourceAmount(ResourceId resourceId, int value)
    {
        if (!resourceId.IsValid) throw new ArgumentException("The resource ID is invalid.", nameof(resourceId));
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        ResourceId = resourceId;
        Value = value;
    }

    public ResourceId ResourceId { get; }
    public int Value { get; }
    public bool IsValid => ResourceId.IsValid && Value >= 0;
}
