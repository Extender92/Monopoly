using Monopoly.Core.Presentation;

namespace Monopoly.Console.GUI;

/// <summary>Maps profile-owned semantic presentation to terminal-local values.</summary>
internal sealed class ConsolePresentationResolver
{
    private readonly ProfilePresentation _presentation;

    internal ConsolePresentationResolver(ProfilePresentation presentation)
    {
        _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
    }

    internal string GetDisplayText(PresentationToken token) => _presentation.ResolveDisplayText(token);

    internal string GetDescription(PresentationToken token) =>
        _presentation.Resolve(token).Description ?? string.Empty;

    internal string FormatAmount(int amount, PresentationToken resourceToken)
    {
        string? symbol = _presentation.Resolve(resourceToken).Symbol;
        return symbol is null ? amount.ToString() : $"{amount}{symbol}";
    }

    internal ConsoleColor GetColor(PresentationToken token)
    {
        PresentationMetadata metadata = _presentation.Resolve(token);
        return GetColorHint(metadata.ColorToken);
    }

    internal static ConsoleColor GetColorHint(PresentationToken? token) => token?.Value switch
    {
        "accent.earth" => ConsoleColor.DarkGray,
        "accent.mist" => ConsoleColor.DarkCyan,
        "accent.bloom" => ConsoleColor.Magenta,
        "accent.ember" => ConsoleColor.DarkYellow,
        "accent.flame" => ConsoleColor.DarkRed,
        "accent.sun" => ConsoleColor.Yellow,
        "accent.grove" => ConsoleColor.DarkGreen,
        "accent.depth" => ConsoleColor.DarkBlue,
        _ => ConsoleColor.White
    };

    internal bool HasKnownLayout(PresentationToken token)
    {
        PresentationToken? layout = _presentation.Resolve(token).LayoutToken;
        return layout?.Value is "layout.corner" or "layout.edge";
    }
}
