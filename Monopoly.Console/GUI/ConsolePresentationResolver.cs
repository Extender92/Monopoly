using Monopoly.Core.Presentation;

namespace Monopoly.Console.GUI;

internal sealed class ConsolePresentationResolver
{
    private readonly ProfilePresentation _presentation;

    internal ConsolePresentationResolver(ProfilePresentation presentation)
    {
        _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
    }

    internal string GetDisplayText(PresentationToken token)
    {
        try
        {
            return ConsoleText.Sanitize(_presentation.ResolveDisplayText(token));
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            throw ConsoleProjectionException.MissingToken(token, exception);
        }
    }

    internal string FormatAmount(int amount, PresentationToken resourceToken)
    {
        try
        {
            PresentationMetadata metadata = _presentation.Resolve(resourceToken);
            string unit = ConsoleText.Sanitize(
                metadata.Symbol ?? metadata.DisplayText ?? metadata.ShortText ?? resourceToken.Value);
            return $"{amount} {unit}";
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            throw ConsoleProjectionException.MissingToken(resourceToken, exception);
        }
    }

    internal ConsoleColor GetColor(PresentationToken token)
    {
        try
        {
            return GetColorHint(_presentation.Resolve(token).ColorToken);
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            throw ConsoleProjectionException.MissingToken(token, exception);
        }
    }

    internal static ConsoleColor GetColorHint(PresentationToken? token) => token?.Value switch
    {
        "accent.moss" => ConsoleColor.DarkGreen,
        "accent.glass" => ConsoleColor.Cyan,
        "accent.river" => ConsoleColor.Blue,
        "accent.copper" => ConsoleColor.DarkYellow,
        "accent.lantern" => ConsoleColor.Yellow,
        "accent.event" => ConsoleColor.DarkCyan,
        "accent.neutral" => ConsoleColor.White,
        _ => ConsoleColor.White
    };
}
