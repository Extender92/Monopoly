using Monopoly.Console.GUI;
using Monopoly.Console.Models;
using Monopoly.Core;
using Monopoly.Core.Interface;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Monopoly.Core.SaveAndLoad;
using System;
using System.Runtime.InteropServices;

namespace Monopoly.Console
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ConsolePositions.SetStandardPositions();
            IConsoleWrapper consoleWrapper = new ConsoleWrapper();
            MenuOptionSelector menu = new MenuOptionSelector(consoleWrapper);
            MainMenu mainMenu = new MainMenu(menu);
            mainMenu.DisplayMainMenu();
        }

        internal static void StartNewGame()
        {
            IConsoleWrapper consoleWrapper = new ConsoleWrapper();

            GameRules gameRules = SetupRules(consoleWrapper);

            Game game = CoreGameSetup.Setup(gameRules);

            ConsolePrinter consolePrinter = new ConsolePrinter(consoleWrapper, game.Board.Squares, gameRules);

            TablePieceInputManager PieceInput = new TablePieceInputManager(consoleWrapper, consolePrinter);

            ConsoleGameSetup gameSetup = new ConsoleGameSetup(gameRules, PieceInput);

            IMenuOptionSelector menu = new MenuOptionSelector(consoleWrapper);

            Input input = new Input(consoleWrapper, menu);

            ConsoleLogPrinter logPrinter = new ConsoleLogPrinter(consoleWrapper);

            ConsoleCardPrinter cardPrinter = new ConsoleCardPrinter(consoleWrapper, game.Board.Squares, gameRules);

            ConsoleGame consoleGame = gameSetup.Setup(game, consolePrinter, input, logPrinter, cardPrinter);

            consoleGame.StartConsoleGame();
        }

        internal static void LoadGame()
        {
            IConsoleWrapper consoleWrapper = new ConsoleWrapper();

            GameRules gameRules = new GameRules(2, 2, 2);

            Game game = CoreGameSetup.Setup(gameRules);

            ConsolePrinter consolePrinter = new ConsolePrinter(consoleWrapper, game.Board.Squares, gameRules);

            TablePieceInputManager PieceInput = new TablePieceInputManager(consoleWrapper, consolePrinter);

            ConsoleGameSetup gameSetup = new ConsoleGameSetup(gameRules, PieceInput);

            IMenuOptionSelector menu = new MenuOptionSelector(consoleWrapper);

            Input input = new Input(consoleWrapper, menu);

            ConsoleLogPrinter logPrinter = new ConsoleLogPrinter(consoleWrapper);

            ConsoleCardPrinter cardPrinter = new ConsoleCardPrinter(consoleWrapper, game.Board.Squares, gameRules);

            ConsoleGame consoleGame = gameSetup.Setup(game, consolePrinter, input, logPrinter, cardPrinter);

            LoadCoreData.LoadData(consoleGame.CurrentGame);

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