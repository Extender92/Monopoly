using Monopoly.Console.GUI;
using Monopoly.Core;
using Monopoly.Core.Persistence;
using Monopoly.Core.Randomness;

namespace Monopoly.Console;

internal sealed class ConsoleGameSession
{
    private readonly IConsoleWrapper _console;
    private readonly IGameSaveStore _saveStore;
    private readonly ConsoleInputReader _input;
    private readonly ConsoleProjectionBuilder _projections = new();
    private readonly ConsoleNotificationFormatter _notifications = new();
    private readonly ConsoleRenderer _renderer;

    internal ConsoleGameSession(IConsoleWrapper console, IGameSaveStore saveStore)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _saveStore = saveStore ?? throw new ArgumentNullException(nameof(saveStore));
        _input = new ConsoleInputReader(console);
        _renderer = new ConsoleRenderer(console);
    }

    internal void Run(Game game, string initialMessage)
    {
        ArgumentNullException.ThrowIfNull(game);
        List<string> messages = [ConsoleText.Sanitize(initialMessage)];
        using ConsoleNotificationBuffer notificationBuffer = new(game.Notifications);

        while (true)
        {
            ConsoleMatchProjection projection;
            try
            {
                projection = _projections.Build(game);
            }
            catch (ConsoleProjectionException exception)
            {
                _console.Clear();
                _renderer.WriteMessage(ProjectionErrorMessage(exception.Kind));
                _renderer.WriteMessage("Press Enter to return to the main menu.");
                _console.ReadLine();
                return;
            }

            _renderer.RenderMatch(projection, messages);
            messages = [];

            try
            {
                switch (game.Phase)
                {
                    case GamePhase.ReadyForTurn:
                        if (!RunReadyCommand(game, projection, notificationBuffer, messages)) return;
                        break;
                    case GamePhase.AwaitingDecision:
                        if (!RunDecisionCommand(game, projection, notificationBuffer, messages)) return;
                        break;
                    case GamePhase.GameOver:
                        if (!RunGameOverCommand(game, projection, messages)) return;
                        break;
                    default:
                        messages.Add("The match is in an unsupported phase.");
                        return;
                }
            }
            catch (ConsoleProjectionException exception)
            {
                ShowProjectionError(exception.Kind);
                return;
            }
        }
    }

    private bool RunReadyCommand(
        Game game,
        ConsoleMatchProjection projection,
        ConsoleNotificationBuffer notificationBuffer,
        List<string> messages)
    {
        int selected = _input.ReadChoice([
            "Play turn",
            "View route",
            "View decks",
            "Save match",
            "Return to main menu"
        ])!.Value;

        switch (selected)
        {
            case 0:
                ExecuteAction(game, game.PlayTurn, notificationBuffer, messages);
                return true;
            case 1:
                _renderer.RenderTrack(projection);
                return true;
            case 2:
                _renderer.RenderDecks(projection);
                return true;
            case 3:
                Save(game, messages);
                return true;
            case 4:
                return false;
            default:
                throw new InvalidOperationException("The ready-state menu returned an unknown option.");
        }
    }

    private bool RunDecisionCommand(
        Game game,
        ConsoleMatchProjection projection,
        ConsoleNotificationBuffer notificationBuffer,
        List<string> messages)
    {
        ConsoleDecisionProjection decision = projection.Decision ?? throw new ConsoleProjectionException(
            ConsoleProjectionErrorKind.InconsistentState,
            "The match is awaiting a decision but exposes no pending decision.");
        List<string> options = decision.Options.Select(option => option.Label).ToList();
        options.AddRange(["View route", "View decks", "Save match", "Return to main menu"]);
        int selected = _input.ReadChoice(options)!.Value;

        if (selected < decision.Options.Count)
        {
            ConsoleDecisionOptionProjection response = decision.Options[selected];
            ExecuteAction(
                game,
                () => game.SubmitDecision(new DecisionResponse(
                    decision.DecisionId,
                    decision.PlayerId,
                    response.Id)),
                notificationBuffer,
                messages);
            return true;
        }

        return (selected - decision.Options.Count) switch
        {
            0 => RenderAndContinue(() => _renderer.RenderTrack(projection)),
            1 => RenderAndContinue(() => _renderer.RenderDecks(projection)),
            2 => SaveAndContinue(game, messages),
            3 => false,
            _ => throw new InvalidOperationException("The decision menu returned an unknown option.")
        };
    }

    private bool RunGameOverCommand(
        Game game,
        ConsoleMatchProjection projection,
        List<string> messages)
    {
        int selected = _input.ReadChoice([
            "View route",
            "View decks",
            "Save final match",
            "Return to main menu"
        ])!.Value;
        return selected switch
        {
            0 => RenderAndContinue(() => _renderer.RenderTrack(projection)),
            1 => RenderAndContinue(() => _renderer.RenderDecks(projection)),
            2 => SaveAndContinue(game, messages),
            3 => false,
            _ => throw new InvalidOperationException("The terminal menu returned an unknown option.")
        };
    }

    private void ExecuteAction(
        Game game,
        Func<GameActionResult> action,
        ConsoleNotificationBuffer notificationBuffer,
        List<string> messages)
    {
        try
        {
            GameActionResult result = action();
            IReadOnlyList<string> notificationMessages = _notifications.Format(game, notificationBuffer.Drain());
            messages.AddRange(notificationMessages);
            if (result.Status == GameActionStatus.Rejected)
                messages.Add(RejectionMessage(result.RejectionReason));
            else if (result.TurnResult is not null)
                messages.Add(TurnResultMessage(game, result.TurnResult));
        }
        catch (RandomSourceException)
        {
            _ = notificationBuffer.Drain();
            messages.Add("The turn could not be completed because the random source failed.");
        }
        catch (ProfileExecutionException)
        {
            _ = notificationBuffer.Drain();
            messages.Add("The turn could not be completed because the profile operation failed.");
        }
    }

    private string TurnResultMessage(Game game, TurnResult result)
    {
        ConsolePresentationResolver presentation = new(game.Presentation);
        string player = ConsoleText.Sanitize(result.Player.Name);
        string space = presentation.GetDisplayText(result.LandedSpace.PresentationToken);
        return $"{player} rolled {string.Join(" + ", result.Roll.Results)} = {result.Roll.Sum} and ended on {space}.";
    }

    private void Save(Game game, List<string> messages)
    {
        try
        {
            _saveStore.Save(game);
            messages.Add("Match saved.");
        }
        catch (SaveStoreException exception)
        {
            messages.Add(exception.Kind switch
            {
                SaveStoreErrorKind.InvalidData => "The active match could not be saved because its state is invalid.",
                SaveStoreErrorKind.StorageFailure => "The save storage could not be written.",
                _ => "The match could not be saved."
            });
        }
    }

    private static string RejectionMessage(GameActionRejectionReason? reason) => reason switch
    {
        GameActionRejectionReason.PendingDecisionRequired => "Resolve the pending decision before playing another turn.",
        GameActionRejectionReason.NoPendingDecision => "There is no pending decision to answer.",
        GameActionRejectionReason.MalformedResponse => "The decision response was malformed.",
        GameActionRejectionReason.StaleDecision => "That decision is no longer current.",
        GameActionRejectionReason.DuplicateDecision => "That decision has already been answered.",
        GameActionRejectionReason.ResponseNotAllowed => "That response is not allowed.",
        GameActionRejectionReason.WrongPlayer => "Only the requested player may answer this decision.",
        GameActionRejectionReason.InsufficientResources => "The player no longer has enough resources.",
        GameActionRejectionReason.DecisionPreconditionFailed => "The decision can no longer be applied to the current state.",
        GameActionRejectionReason.OperationInProgress => "Another match operation is already in progress.",
        _ => "The match action was rejected."
    };

    private static string ProjectionErrorMessage(ConsoleProjectionErrorKind kind) => kind switch
    {
        ConsoleProjectionErrorKind.MissingPresentation =>
            "The match cannot be displayed because required profile presentation metadata is missing.",
        ConsoleProjectionErrorKind.UnsupportedDecision =>
            "The match requires a decision that this Console version does not support.",
        ConsoleProjectionErrorKind.UnsupportedCapability =>
            "The match contains a capability that this Console version does not support.",
        _ => "The match state cannot be projected safely."
    };

    private void ShowProjectionError(ConsoleProjectionErrorKind kind)
    {
        _console.Clear();
        _renderer.WriteMessage(ProjectionErrorMessage(kind));
        _renderer.WriteMessage("Press Enter to return to the main menu.");
        _console.ReadLine();
    }

    private static bool RenderAndContinue(Action render)
    {
        render();
        return true;
    }

    private bool SaveAndContinue(Game game, List<string> messages)
    {
        Save(game, messages);
        return true;
    }
}
