using System.Text.RegularExpressions;

namespace Monopoly.Core;

internal static class StructuralIdentifier
{
    internal const int MaximumLength = 128;

    private static readonly Regex ValidValue = new(
        "^[a-z][a-z0-9]*(?:[.-][a-z0-9]+)*$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    internal static string Validate(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length > MaximumLength ||
            !ValidValue.IsMatch(value))
        {
            throw new ArgumentException(
                $"A structural identifier must be at most {MaximumLength} characters and contain lowercase ASCII segments separated by '.' or '-'.",
                parameterName);
        }

        return value;
    }

    internal static bool IsValid(string? value) =>
        value is not null && value.Length <= MaximumLength && ValidValue.IsMatch(value);
}

public readonly struct SpaceId : IEquatable<SpaceId>, IComparable<SpaceId>
{
    public const int MaximumLength = StructuralIdentifier.MaximumLength;

    public SpaceId(string value) => Value = StructuralIdentifier.Validate(value, nameof(value));

    public string Value { get; }
    public bool IsValid => StructuralIdentifier.IsValid(Value);
    public int CompareTo(SpaceId other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public bool Equals(SpaceId other) => StringComparer.Ordinal.Equals(Value, other.Value);
    public override bool Equals(object? obj) => obj is SpaceId other && Equals(other);
    public override int GetHashCode() => Value is null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value ?? string.Empty;
    public static bool operator ==(SpaceId left, SpaceId right) => left.Equals(right);
    public static bool operator !=(SpaceId left, SpaceId right) => !left.Equals(right);
}

public readonly struct DeckId : IEquatable<DeckId>, IComparable<DeckId>
{
    public const int MaximumLength = StructuralIdentifier.MaximumLength;

    public DeckId(string value) => Value = StructuralIdentifier.Validate(value, nameof(value));

    public string Value { get; }
    public bool IsValid => StructuralIdentifier.IsValid(Value);
    public int CompareTo(DeckId other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public bool Equals(DeckId other) => StringComparer.Ordinal.Equals(Value, other.Value);
    public override bool Equals(object? obj) => obj is DeckId other && Equals(other);
    public override int GetHashCode() => Value is null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value ?? string.Empty;
    public static bool operator ==(DeckId left, DeckId right) => left.Equals(right);
    public static bool operator !=(DeckId left, DeckId right) => !left.Equals(right);
}

public readonly struct CardId : IEquatable<CardId>, IComparable<CardId>
{
    public const int MaximumLength = StructuralIdentifier.MaximumLength;

    public CardId(string value) => Value = StructuralIdentifier.Validate(value, nameof(value));

    public string Value { get; }
    public bool IsValid => StructuralIdentifier.IsValid(Value);
    public int CompareTo(CardId other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public bool Equals(CardId other) => StringComparer.Ordinal.Equals(Value, other.Value);
    public override bool Equals(object? obj) => obj is CardId other && Equals(other);
    public override int GetHashCode() => Value is null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value ?? string.Empty;
    public static bool operator ==(CardId left, CardId right) => left.Equals(right);
    public static bool operator !=(CardId left, CardId right) => !left.Equals(right);
}
