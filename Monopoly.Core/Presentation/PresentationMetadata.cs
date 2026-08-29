namespace Monopoly.Core.Presentation;

/// <summary>Optional, frontend-neutral presentation owned by a profile.</summary>
public sealed class PresentationMetadata
{
    public PresentationMetadata(
        PresentationToken token,
        string? displayText = null,
        string? shortText = null,
        string? description = null,
        string? symbol = null,
        PresentationToken? colorToken = null,
        PresentationToken? layoutToken = null)
    {
        if (!token.IsValid) throw new ArgumentException("The metadata token is invalid.", nameof(token));

        Token = token;
        DisplayText = Normalize(displayText);
        ShortText = Normalize(shortText);
        Description = Normalize(description);
        Symbol = Normalize(symbol);
        ColorToken = ValidateOptionalToken(colorToken, nameof(colorToken));
        LayoutToken = ValidateOptionalToken(layoutToken, nameof(layoutToken));
    }

    public PresentationToken Token { get; }
    public string? DisplayText { get; }
    public string? ShortText { get; }
    public string? Description { get; }
    public string? Symbol { get; }
    public PresentationToken? ColorToken { get; }
    public PresentationToken? LayoutToken { get; }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static PresentationToken? ValidateOptionalToken(PresentationToken? token, string parameterName)
    {
        if (token is { IsValid: false })
            throw new ArgumentException("The referenced presentation token is invalid.", parameterName);

        return token;
    }
}
