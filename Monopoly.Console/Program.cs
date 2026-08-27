using Monopoly.Console.GUI;
using Monopoly.Console.Models;
using Infrastructure.Persistence;
using Monopoly.Core;
using Monopoly.Core.Interface;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Persistence;
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
        {
            ArgumentNullException.ThrowIfNull(saveStore);
            IConsoleWrapper consoleWrapper = new ConsoleWrapper();

            GameRules gameRules = SetupRules(consoleWrapper);

            Game game = CoreGameSetup.Setup(gameRules);

            ConsolePrinter consolePrinter = new ConsolePrinter(consoleWrapper, game.Board.Squares, gameRules);

            TablePieceInputManager PieceInput = new TablePieceInputManager(consoleWrapper, consolePrinter);

            ConsoleGameSetup gameSetup = new ConsoleGameSetup(gameRules, PieceInput);

            IMenuOptionSelector menu = new MenuOptionSelector(consoleWrapper);

            Input input = new Input(consoleWrapper, menu);

            game.Decisions = new ConsolePlayerDecisionProvider(consolePrinter, input, gameRules, saveStore);

            ConsoleLogPrinter logPrinter = new ConsoleLogPrinter(consoleWrapper);

            ConsoleCardPrinter cardPrinter = new ConsoleCardPrinter(consoleWrapper, game.Board.Squares, gameRules);

            ConsoleGame consoleGame = gameSetup.Setup(game, consolePrinter, input, logPrinter, cardPrinter, saveStore);

            consoleGame.StartConsoleGame();
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
                game = saveStore.Load();
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

            ConsolePrinter consolePrinter = new ConsolePrinter(consoleWrapper, game.Board.Squares, gameRules);

            TablePieceInputManager PieceInput = new TablePieceInputManager(consoleWrapper, consolePrinter);

            ConsoleGameSetup gameSetup = new ConsoleGameSetup(gameRules, PieceInput);

            IMenuOptionSelector menu = new MenuOptionSelector(consoleWrapper);

            Input input = new Input(consoleWrapper, menu);

            game.Decisions = new ConsolePlayerDecisionProvider(consolePrinter, input, gameRules, saveStore);

            ConsoleLogPrinter logPrinter = new ConsoleLogPrinter(consoleWrapper);

            ConsoleCardPrinter cardPrinter = new ConsoleCardPrinter(consoleWrapper, game.Board.Squares, gameRules);

            ConsoleGame consoleGame = gameSetup.Setup(game, consolePrinter, input, logPrinter, cardPrinter, saveStore);

            consoleGame.StartConsoleGame();
        }

        private static GameRules SetupRules(IConsoleWrapper consoleWrapper)
        {
            IMenuOptionSelector menu = new MenuOptionSelector(consoleWrapper);
            Input input = new Input(consoleWrapper, menu);
            int numberOfDice = 2;
            int dieSides = 6;
            int numberOfPlayers = input.GetNumberOfPlayers();
            return new GameRules(numberOfPlayers, numberOfDice, dieSides);
        }
    }
}
