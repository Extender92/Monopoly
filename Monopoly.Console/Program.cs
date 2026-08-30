using Infrastructure.Persistence;
using Infrastructure.Profiles;
using Monopoly.Console.GUI;
using Monopoly.Core;
using Monopoly.Core.Persistence;
using Monopoly.Core.Randomness;

namespace Monopoly.Console;

internal static class Program
{
    private const string Usage = "Usage: Monopoly.Console [--profile <path>] [--help]";
    private const string SessionTransitionMessage =
        "The selected profile is valid and supported. Interactive match play is temporarily unavailable while generic Console projections are being completed.";

    private static int Main(string[] args) => Run(args, new ConsoleWrapper());

    internal static int Run(
        string[] args,
        IConsoleWrapper consoleWrapper,
        Action<ValidatedGameProfile, IConsoleWrapper>? runApplication = null)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(consoleWrapper);

        ConsoleCommandLineOptions options;
        try
        {
            options = ConsoleCommandLineOptions.Parse(args);
        }
        catch (ConsoleCommandLineException exception)
        {
            consoleWrapper.WriteLine(exception.Message);
            consoleWrapper.WriteLine(Usage);
            return 2;
        }

        if (options.ShowHelp)
        {
            consoleWrapper.WriteLine(Usage);
            return 0;
        }

        ValidatedGameProfile profile;
        try
        {
            profile = LoadSelectedProfile(options.ProfilePath);
        }
        catch (ProfileSourceException exception)
        {
            consoleWrapper.WriteLine(SourceErrorMessage(exception.Kind));
            return 1;
        }
        catch (ProfileJsonException exception)
        {
            consoleWrapper.WriteLine(JsonErrorMessage(exception.Kind));
            return 1;
        }
        catch (ProfileValidationException)
        {
            consoleWrapper.WriteLine("The profile content is invalid.");
            return 1;
        }
        catch (GameSetupException exception) when (
            exception.Kind is GameSetupErrorKind.UnsupportedComponent or GameSetupErrorKind.UnsupportedPolicy)
        {
            consoleWrapper.WriteLine("The profile uses components that this engine version does not support.");
            return 1;
        }
        catch (GameSetupException)
        {
            consoleWrapper.WriteLine("The profile is not compatible with match setup.");
            return 1;
        }

        (runApplication ?? DisplayMainMenu)(profile, consoleWrapper);
        return 0;
    }

    internal static void StartNewGame(IGameSaveStore saveStore, ValidatedGameProfile profile) =>
        StartNewGame(saveStore, profile, new ConsoleWrapper());

    internal static void StartNewGame(
        IGameSaveStore saveStore,
        ValidatedGameProfile profile,
        IConsoleWrapper consoleWrapper)
    {
        ArgumentNullException.ThrowIfNull(saveStore);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(consoleWrapper);
        GameSetup.ValidateCompatibility(profile);
        ShowTransitionMessage(consoleWrapper, SessionTransitionMessage);
    }

    internal static ValidatedGameProfile LoadBundledDemoProfile() => LoadSelectedProfile(null);

    internal static ValidatedGameProfile LoadSelectedProfile(string? explicitProfilePath)
    {
        string path = explicitProfilePath ?? Path.Combine(
            AppContext.BaseDirectory,
            "profiles",
            "demo",
            "lantern-vale-v1.json");
        ValidatedGameProfile profile = new JsonFileGameProfileSource(path).Load();
        GameSetup.ValidateCompatibility(profile);
        return profile;
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

    private static void DisplayMainMenu(
        ValidatedGameProfile profile,
        IConsoleWrapper consoleWrapper)
    {
        ConsolePositions.SetStandardPositions();
        MainMenu mainMenu = new(
            new MenuOptionSelector(consoleWrapper),
            new JsonFileGameSaveStore("game_data.json"),
            profile);
        mainMenu.DisplayMainMenu();
    }

    private static string SourceErrorMessage(ProfileSourceErrorKind kind) => kind switch
    {
        ProfileSourceErrorKind.NotFound => "The profile file was not found.",
        ProfileSourceErrorKind.AccessDenied => "Access to the profile file was denied.",
        ProfileSourceErrorKind.InvalidPath => "The profile path is invalid.",
        ProfileSourceErrorKind.StorageFailure => "The profile file could not be read.",
        _ => "The profile source could not be loaded."
    };

    private static string JsonErrorMessage(ProfileJsonErrorKind kind) => kind switch
    {
        ProfileJsonErrorKind.InputTooLarge => "The profile file exceeds the supported size limit.",
        ProfileJsonErrorKind.InvalidEncoding => "The profile file must use valid UTF-8.",
        ProfileJsonErrorKind.MalformedJson => "The profile file contains malformed JSON.",
        ProfileJsonErrorKind.DepthExceeded => "The profile JSON exceeds the supported depth limit.",
        ProfileJsonErrorKind.UnsupportedSchemaVersion => "The profile uses an unsupported schema version.",
        ProfileJsonErrorKind.UnknownMember or
        ProfileJsonErrorKind.DuplicateMember or
        ProfileJsonErrorKind.InvalidWireValue => "The profile JSON does not match the supported schema.",
        _ => "The profile JSON could not be loaded."
    };
}
