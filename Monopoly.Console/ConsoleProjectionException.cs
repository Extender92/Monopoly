using Monopoly.Core.Presentation;

namespace Monopoly.Console;

internal enum ConsoleProjectionErrorKind
{
    MissingPresentation,
    UnsupportedDecision,
    UnsupportedCapability,
    InconsistentState
}

internal sealed class ConsoleProjectionException : Exception
{
    internal ConsoleProjectionException(
        ConsoleProjectionErrorKind kind,
        string message,
        Exception? innerException = null)
        : base(message, innerException) => Kind = kind;

    internal ConsoleProjectionErrorKind Kind { get; }

    internal static ConsoleProjectionException MissingToken(
        PresentationToken token,
        Exception? innerException = null) =>
        new(
            ConsoleProjectionErrorKind.MissingPresentation,
            $"Required presentation token '{token}' is missing.",
            innerException);
}
