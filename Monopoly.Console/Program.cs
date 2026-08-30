using Infrastructure.Persistence;
using Infrastructure.Profiles;
using Monopoly.Console.GUI;
using Monopoly.Core;
using Monopoly.Core.Persistence;
using Monopoly.Core.Randomness;

namespace Monopoly.Console;

internal static class Program
{
    private const string SessionTransitionMessage =
        "Demo capability execution is available in Core. Interactive match play is temporarily unavailable while generic Console projections are being completed.";

    private static void Main()
    {
        ConsolePositions.SetStandardPositions();
        IConsoleWrapper consoleWrapper = new ConsoleWrapper();
        MainMenu mainMenu = new(new MenuOptionSelector(consoleWrapper), new JsonFileGameSaveStore("game_data.json"));
        mainMenu.DisplayMainMenu();
    }

    internal static void StartNewGame(IGameSaveStore saveStore) =>
        StartNewGame(saveStore, new ConsoleWrapper());

    internal static void StartNewGame(IGameSaveStore saveStore, IConsoleWrapper consoleWrapper)
    {
        ArgumentNullException.ThrowIfNull(saveStore);
        ArgumentNullException.ThrowIfNull(consoleWrapper);
        _ = LoadBundledDemoProfile();
        ShowTransitionMessage(consoleWrapper, SessionTransitionMessage);
    }

    internal static ValidatedGameProfile LoadBundledDemoProfile()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "profiles", "demo", "lantern-vale-v1.json");
        return new JsonGameProfileParser().Parse(File.ReadAllBytes(path));
    }

    internal static void LoadGame(IGameSaveStore saveStore) =>
        LoadGame(saveStore, new ConsoleWrapper());

    internal static void LoadGame(IGameSaveStore saveStore, IConsoleWrapper consoleWrapper)
    {
        ArgumentNullException.ThrowIfNull(saveStore);
        ArgumentNullException.ThrowIfNull(consoleWrapper);

        try
        {
            _ = saveStore.Load(randomSource: new SystemMatchRandomSource());
            ShowTransitionMessage(consoleWrapper, "Loaded matches cannot enter the Console session until generic projections are completed.");
        }
        catch (SaveStoreException exception)
        {
            string message = exception.Kind switch
            {
                SaveStoreErrorKind.NotFound => "No save file was found.",
                SaveStoreErrorKind.InvalidData => "The save file contains invalid data.",
                SaveStoreErrorKind.IncompatibleVersion => "The save file uses an unsupported version.",
                SaveStoreErrorKind.StorageFailure => "The save storage could not be accessed.",
                _ => "The save file could not be loaded."
            };
            ShowTransitionMessage(consoleWrapper, message);
        }
    }

    private static void ShowTransitionMessage(IConsoleWrapper consoleWrapper, string message)
    {
        consoleWrapper.WriteLine($"{message} Press Enter to return to the main menu.");
        consoleWrapper.ReadLine();
    }
}
