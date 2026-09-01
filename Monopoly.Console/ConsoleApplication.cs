using Monopoly.Console.GUI;
using Monopoly.Core;
using Monopoly.Core.Persistence;
using Monopoly.Core.Randomness;

namespace Monopoly.Console;

internal sealed class ConsoleApplication
{
    private readonly ValidatedGameProfile _profile;
    private readonly IConsoleWrapper _console;
    private readonly IGameSaveStore _saveStore;
    private readonly Func<IMatchRandomSource> _randomSourceFactory;
    private readonly ConsoleInputReader _input;
    private readonly ConsoleGameSession _session;

    internal ConsoleApplication(
        ValidatedGameProfile profile,
        IConsoleWrapper console,
        IGameSaveStore saveStore,
        Func<IMatchRandomSource> randomSourceFactory)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _saveStore = saveStore ?? throw new ArgumentNullException(nameof(saveStore));
        _randomSourceFactory = randomSourceFactory ?? throw new ArgumentNullException(nameof(randomSourceFactory));
        _input = new ConsoleInputReader(console);
        _session = new ConsoleGameSession(console, saveStore);
    }

    internal void Run()
    {
        string? message = null;
        ConsolePresentationResolver presentation = new(_profile.Presentation);
        string profileName = presentation.GetDisplayText(_profile.PresentationToken);

        while (true)
        {
            _console.Clear();
            _console.WriteLine($"Selected profile: {profileName}");
            if (message is not null)
            {
                _console.WriteLine(message);
                message = null;
            }
            _console.WriteLine(string.Empty);

            int selected = _input.ReadChoice(["Start new match", "Load saved match", "Exit"])!.Value;
            switch (selected)
            {
                case 0:
                    message = StartNewMatch();
                    break;
                case 1:
                    message = LoadMatch();
                    break;
                case 2:
                    return;
                default:
                    throw new InvalidOperationException("The main menu returned an unknown option.");
            }
        }
    }

    private string? StartNewMatch()
    {
        _console.Clear();
        IReadOnlyList<PlayerSetup>? players = _input.ReadPlayers(_profile.Setup);
        if (players is null) return "Match setup was cancelled.";

        try
        {
            Game game = GameSetup.Create(_profile, players, _randomSourceFactory());
            _session.Run(game, "New match created.");
            return null;
        }
        catch (GameSetupException exception)
        {
            return exception.Kind switch
            {
                GameSetupErrorKind.InvalidPlayerCount => "The selected profile does not allow that player count.",
                GameSetupErrorKind.InvalidPlayer or GameSetupErrorKind.DuplicatePlayer => "The player setup is invalid.",
                GameSetupErrorKind.UnsupportedComponent or GameSetupErrorKind.UnsupportedPolicy =>
                    "The selected profile uses components that this engine version does not support.",
                _ => "The match could not be created from the selected profile."
            };
        }
        catch (RandomSourceException)
        {
            return "The match could not be created because the random source failed.";
        }
    }

    private string? LoadMatch()
    {
        try
        {
            Game game = _saveStore.Load(
                new GameProfileRegistry([_profile]),
                _randomSourceFactory());
            _session.Run(game, "Saved match loaded.");
            return null;
        }
        catch (SaveStoreException exception)
        {
            return exception.Kind switch
            {
                SaveStoreErrorKind.NotFound => "No save file was found.",
                SaveStoreErrorKind.InvalidData => "The save file contains invalid data.",
                SaveStoreErrorKind.IncompatibleVersion => "The save file uses an unsupported version.",
                SaveStoreErrorKind.IncompatibleProfile => "The save requires a different or changed profile.",
                SaveStoreErrorKind.StorageFailure => "The save storage could not be accessed.",
                _ => "The save file could not be loaded."
            };
        }
    }
}
