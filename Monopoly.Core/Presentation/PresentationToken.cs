using System.Text.RegularExpressions;

namespace Monopoly.Core.Presentation;

/// <summary>A stable semantic key used to resolve profile-owned presentation.</summary>
public readonly struct PresentationToken : IEquatable<PresentationToken>, IComparable<PresentationToken>
{
    public const int MaximumLength = 128;

    private static readonly Regex ValidToken = new(
        "^[a-z][a-z0-9]*(?:[.-][a-z0-9]+)*$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    public PresentationToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaximumLength || !ValidToken.IsMatch(value))
        {
            throw new ArgumentException(
                $"A presentation token must be at most {MaximumLength} characters and contain lowercase ASCII segments separated by '.' or '-'.",
                nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public bool IsValid => Value is not null && Value.Length <= MaximumLength && ValidToken.IsMatch(Value);

    public int CompareTo(PresentationToken other) => StringComparer.Ordinal.Compare(Value, other.Value);

    public bool Equals(PresentationToken other) => StringComparer.Ordinal.Equals(Value, other.Value);

    public override bool Equals(object? obj) => obj is PresentationToken other && Equals(other);

    public override int GetHashCode() => Value is null ? 0 : StringComparer.Ordinal.GetHashCode(Value);

    public override string ToString() => Value ?? string.Empty;

    public static bool operator ==(PresentationToken left, PresentationToken right) => left.Equals(right);

    public static bool operator !=(PresentationToken left, PresentationToken right) => !left.Equals(right);
}
