using Monopoly.Console.GUI;
using Monopoly.Console.Models;
using Infrastructure.Persistence;
using Infrastructure.Profiles;
using Monopoly.Core;
using Monopoly.Core.Interface;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Persistence;
using Monopoly.Core.Randomness;
using System;

namespace Monopoly.Console
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ConsolePositions.SetStandardPositions();
            IConsoleWrapper consoleWrapper = new ConsoleWrapper();
            MenuOptionSelector menu = new MenuOptionSelector(consoleWrapper);
            IGameSaveStore saveStore = new JsonFileGameSaveStore("game_data.json");
            MainMenu mainMenu = new MainMenu(menu, saveStore);
            mainMenu.DisplayMainMenu();
        }

        internal static void StartNewGame(IGameSaveStore saveStore)
            => StartNewGame(saveStore, new ConsoleWrapper());

        internal static void StartNewGame(IGameSaveStore saveStore, IConsoleWrapper consoleWrapper)
        {
            ArgumentNullException.ThrowIfNull(saveStore);
            ArgumentNullException.ThrowIfNull(consoleWrapper);
            _ = LoadBundledDemoProfile();
            ShowTransitionMessage(
                consoleWrapper,
                "Match play is temporarily unavailable while validated Demo capability execution is being completed.");
        }

        internal static ValidatedGameProfile LoadBundledDemoProfile()
        {
            string path = Path.Combine(
                AppContext.BaseDirectory,
                "profiles",
                "demo",
                "lantern-vale-v1.json");
            return new JsonGameProfileParser().Parse(File.ReadAllBytes(path));
        }

        internal static void LoadGame(IGameSaveStore saveStore) =>
            LoadGame(saveStore, new ConsoleWrapper());

        internal static void LoadGame(IGameSaveStore saveStore, IConsoleWrapper consoleWrapper)
        {
            ArgumentNullException.ThrowIfNull(saveStore);
            ArgumentNullException.ThrowIfNull(consoleWrapper);

            Game game;
            try
            {
                game = saveStore.Load(randomSource: new SystemMatchRandomSource());
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
                consoleWrapper.WriteLine($"{message} Press Enter to return to the main menu.");
                consoleWrapper.ReadLine();
                return;
            }
            GameRules gameRules = game.Rules;

            ConsolePrinter consolePrinter = new ConsolePrinter(consoleWrapper, game);

            TablePieceInputManager PieceInput = new TablePieceInputManager(consoleWrapper, consolePrinter);

            ConsoleGameSetup gameSetup = new ConsoleGameSetup(gameRules, PieceInput);

            IMenuOptionSelector menu = new MenuOptionSelector(consoleWrapper);

            Input input = new Input(consoleWrapper, menu);

            ConsolePlayerDecisionProvider decisionProvider = new(consolePrinter, input, game, saveStore);
            game.SetDecisionProvider(decisionProvider);

            ConsoleLogPrinter logPrinter = new ConsoleLogPrinter(consoleWrapper);

            ConsoleCardPrinter cardPrinter = new ConsoleCardPrinter(consoleWrapper, game);

            ConsoleGame consoleGame = gameSetup.Setup(game, consolePrinter, input, logPrinter, cardPrinter, saveStore, decisionProvider);

            consoleGame.StartConsoleGame();
        }

        private static void ShowTransitionMessage(IConsoleWrapper consoleWrapper, string message)
        {
            consoleWrapper.WriteLine($"{message} Press Enter to return to the main menu.");
            consoleWrapper.ReadLine();
        }
    }
}
