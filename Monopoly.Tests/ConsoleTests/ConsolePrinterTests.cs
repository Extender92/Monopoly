using Microsoft.VisualStudio.TestPlatform.Utilities;
using Monopoly.Console.GUI;
using Monopoly.Console.Models;
using Monopoly.Core;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Moq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Tests.ConsoleTests
{
    public class ConsolePrinterTests
    {
        private List<Square> GenerateMockSquares()
        {
            var squares = new List<Square>
            {
                new GoSquare(0, "GO", "GO"),
                new PropertySquare(ConsoleColor.Red, "Property", 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 1),
                new TaxSquare(2, 1, "Tax", "Tax"),
                new RailroadSquare(3, "Railroad", 10, 1,2,3,4,5),
                new ParkingSquare(4, "Parking"),
                new JailSquare(5, "Jail", "Jail", "InJail"),
                new GoToJailSquare(6, "GoToJail", "GoToJail"),
                new CommunityChestSquare(7, "Community Chest", "Community Chest"),
                new ChanceSquare(8, "Chance", "Chance"),
                new PropertySquare(ConsoleColor.Red, "Property", 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 9),
                new PropertySquare(ConsoleColor.Red, "Property", 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 10),
                new PropertySquare(ConsoleColor.Red, "Property", 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 11),
                new PropertySquare(ConsoleColor.Red, "Property", 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 12),
                new GoSquare(13, "GO", "GO"),
                new GoSquare(14, "GO", "GO"),
                new GoSquare(15, "GO", "GO"),
                new GoSquare(16, "GO", "GO"),
                new GoSquare(17, "GO", "GO"),
                new GoSquare(18, "GO", "GO"),
                new GoSquare(19, "GO", "GO"),
                new GoSquare(20, "GO", "GO"),
                new GoSquare(21, "GO", "GO"),
                new GoSquare(22, "GO", "GO"),
                new GoSquare(23, "GO", "GO"),
                new GoSquare(24, "GO", "GO"),
                new GoSquare(25, "GO", "GO"),
                new GoSquare(26, "GO", "GO"),
                new GoSquare(27, "GO", "GO"),
                new GoSquare(28, "GO", "GO"),
                new GoSquare(29, "GO", "GO"),
                new GoSquare(30, "GO", "GO"),
                new GoSquare(31, "GO", "GO"),
                new GoSquare(32, "GO", "GO"),
                new GoSquare(33, "GO", "GO"),
                new GoSquare(34, "GO", "GO"),
                new GoSquare(35, "GO", "GO"),
                new GoSquare(36, "GO", "GO"),
                new GoSquare(37, "GO", "GO"),
                new GoSquare(38, "GO", "GO"),
                new GoSquare(39, "GO", "GO"),
            };
            return squares;
        }

        private GameRules GenerateMockGameRules()
        {
            var rules = new GameRules(2, 2, 6)
            {
                GameLanguage = GameRules.Language.UK,
                Salary = 100,
                DoubleOnGo = false,
                FreeParking = GameRules.Parking.Classic,
                MortgageInterestRate = 10
            };
            return rules;
        }

        private List<Player> GenerateMockPlayers()
        {
            List<Player> players = 
            [
                new Player("Player 1", 0),
                new Player("Player 2", 1)
            ];
            return players;
        }

        private List<TablePiece> GenerateMockTablePieces()
        {
            List<TablePiece> tablePieces =
            [
                new TablePiece() { PlayerId = 0, Piece = "K", Color = ConsoleColor.Red },
                new TablePiece() { PlayerId = 1, Piece = "G", Color = ConsoleColor.Blue },
            ];
            return tablePieces;
        }

        [Fact]
        public void PrintGameBoard_CanPrintBoardCorrectly()
        {
            // Arrange
            var consoleMock = new Mock<IConsoleWrapper>();

            var output = new StringWriter();
            System.Console.SetOut(output);
            consoleMock.Setup(c => c.Write(It.IsAny<string>())).Callback<string>(s => output.Write(s));

            var squares = GenerateMockSquares();
            var rules = GenerateMockGameRules();

            var consolePrinter = new ConsolePrinter(consoleMock.Object, squares, rules);

            List<Player> players = GenerateMockPlayers();
            List<TablePiece> tablePieces = GenerateMockTablePieces();

            // Act
            consolePrinter.PrintGameBoard(tablePieces, players);

            // Assert
            consoleMock.Verify(c => c.SetPosition(It.IsAny<int>(), It.IsAny<int>()), Times.AtLeastOnce);
            consoleMock.Verify(c => c.SetTextColor(It.IsAny<ConsoleColor>()), Times.AtLeastOnce);
            consoleMock.Verify(c => c.Write(It.IsAny<string>()), Times.AtLeastOnce);
            consoleMock.Verify(c => c.ResetColor(), Times.AtLeastOnce);

            // Should print [ ] Square length - 1 for [KG] where players is
            string expectedOutput = "[KG]" + string.Concat(Enumerable.Repeat("[ ]", squares.Count - 1));

            Assert.Equal(expectedOutput, output.ToString().Trim());

            // Clean up: Restore console output
            System.Console.SetOut(System.Console.Out);
        }

        [Fact]
        public void PrintSingleSide_CanPrintSingleSideCorrectly()
        {
            // Arrange
            var consoleMock = new Mock<IConsoleWrapper>();

            var squares = GenerateMockSquares();
            var rules = GenerateMockGameRules();

            var consolePrinter = new ConsolePrinter(consoleMock.Object, squares, rules);

            List<Player> players = GenerateMockPlayers();
            List<TablePiece> tablePieces = GenerateMockTablePieces();

            // Set up the parameters for PrintSingleSide
            int playerBuffer = 1;
            int side = 0;
            int startSidePosition = 0;

            // Act
            consolePrinter.PrintSingleSide(playerBuffer, side, startSidePosition, players, tablePieces);

            // Assert
            consoleMock.Verify(c => c.SetPosition(It.IsAny<int>(), It.IsAny<int>()), Times.AtLeastOnce);
            consoleMock.Verify(c => c.SetTextColor(It.IsAny<ConsoleColor>()), Times.AtLeastOnce);
            consoleMock.Verify(c => c.Write(It.IsAny<string>()), Times.AtLeastOnce);
            consoleMock.Verify(c => c.ResetColor(), Times.AtLeastOnce);
        }

        [Theory]
        [InlineData(0, 0, 0, 30, 10)]
        [InlineData(1, 2, 1, 0, 8)]
        [InlineData(2, 4, 2, 20, 0)]
        [InlineData(3, 10, 4, 70, 10)]
        public void GetPositionCoordinates_ShouldCalculateBoardCoordinates(int side, int positionIndex, int playerBuffer, int expectedX, int expectedY)
        {
            // Arrange
            var consoleMock = new Mock<IConsoleWrapper>();

            var squares = GenerateMockSquares();
            var rules = GenerateMockGameRules();

            var consolePrinter = new ConsolePrinter(consoleMock.Object, squares, rules);

            // Act
            consolePrinter.GetPositionCoordinates(side, positionIndex, playerBuffer, out int actualX, out int actualY);

            // Assert
            Assert.Equal(expectedX, actualX);
            Assert.Equal(expectedY, actualY);
        }

        [Fact]
        public void GetPositionCoordinates_ShouldThrowExceptionForInvalidSide()
        {
            // Arrange
            var consoleMock = new Mock<IConsoleWrapper>();

            var squares = GenerateMockSquares();
            var rules = GenerateMockGameRules();

            var consolePrinter = new ConsolePrinter(consoleMock.Object, squares, rules);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => consolePrinter.GetPositionCoordinates(4, 0, 0, out _, out _));
        }

        [Fact]
        public void PrintPositionContent_CanPrintWithPlayersOnPosition()
        {
            // Arrange
            var consoleMock = new Mock<IConsoleWrapper>();

            var squares = GenerateMockSquares();
            var rules = GenerateMockGameRules();

            var consolePrinter = new ConsolePrinter(consoleMock.Object, squares, rules);

            List<Player> players = GenerateMockPlayers();
            List<TablePiece> tablePieces = GenerateMockTablePieces();

            // Act
            consolePrinter.PrintPositionContent(players, tablePieces, ConsoleColor.Green);

            // Assert
            consoleMock.Verify(c => c.SetTextColor(It.IsAny<ConsoleColor>()), Times.Exactly(players.Count + 2)); // +2 for "[" and "]"
            consoleMock.Verify(c => c.Write(It.IsAny<string>()), Times.Exactly(players.Count + 2 )); // *2 for each player's piece
            consoleMock.Verify(c => c.ResetColor(), Times.Exactly(players.Count + 2)); // +2 for "[" and "]"
        }

        [Fact]
        public void PrintPositionContent_CanPrintWithoutPlayersOnPosition()
        {
            // Arrange
            var consoleMock = new Mock<IConsoleWrapper>();

            var squares = GenerateMockSquares();
            var rules = GenerateMockGameRules();

            var consolePrinter = new ConsolePrinter(consoleMock.Object, squares, rules);

            List<Player> players = new List<Player>();
            List<TablePiece> tablePieces = new List<TablePiece>();

            // Act
            consolePrinter.PrintPositionContent(players, tablePieces, ConsoleColor.Green);

            // Assert
            consoleMock.Verify(c => c.SetTextColor(It.IsAny<ConsoleColor>()), Times.Exactly(2));
            consoleMock.Verify(c => c.Write(It.IsAny<string>()), Times.Exactly(2));
            consoleMock.Verify(c => c.ResetColor(), Times.Exactly(2));
        }

        [Fact]
        public void PrintPlayers_CanPrintAllPlayers()
        {
            // Arrange
            var consoleMock = new Mock<IConsoleWrapper>();

            var squares = GenerateMockSquares();
            var rules = GenerateMockGameRules();

            var consolePrinter = new ConsolePrinter(consoleMock.Object, squares, rules);

            List<Player> players = GenerateMockPlayers();
            List<TablePiece> tablePieces = GenerateMockTablePieces();

            // Act
            consolePrinter.PrintPlayers(players, tablePieces);

            // Assert
            consoleMock.Verify(c => c.SetTextColor(It.IsAny<ConsoleColor>()), Times.Exactly(players.Count));
            consoleMock.Verify(c => c.Write(It.IsAny<string>()), Times.Exactly(players.Count));
            consoleMock.Verify(c => c.ResetColor(), Times.Exactly(players.Count));
        }

        [Fact]
        public void PrintPlayers_DontPrintIfNoPlayers()
        {
            // Arrange
            var consoleMock = new Mock<IConsoleWrapper>();

            var squares = GenerateMockSquares();
            var rules = GenerateMockGameRules();

            var consolePrinter = new ConsolePrinter(consoleMock.Object, squares, rules);

            List<Player> players = new List<Player>();
            List<TablePiece> tablePieces = new List<TablePiece>();

            // Act
            consolePrinter.PrintPlayers(players, tablePieces);

            // Assert
            consoleMock.Verify(c => c.SetTextColor(It.IsAny<ConsoleColor>()), Times.Never);
            consoleMock.Verify(c => c.Write(It.IsAny<string>()), Times.Never);
            consoleMock.Verify(c => c.ResetColor(), Times.Never);
        }

        [Fact]
        public void DisplayPlayersInformation_CorrectlyDisplaysPlayerInformation()
        {
            // Arrange
            var consoleMock = new Mock<IConsoleWrapper>();

            var output = new StringWriter();
            System.Console.SetOut(output);
            consoleMock.Setup(c => c.Write(It.IsAny<string>())).Callback<string>(s => output.Write(s));

            var squares = GenerateMockSquares();
            var rules = GenerateMockGameRules();

            var consolePrinter = new ConsolePrinter(consoleMock.Object, squares, rules);

            List<Player> players = GenerateMockPlayers();

            // Act
            consolePrinter.DisplayPlayersInformation(players[0], players);

            // Assert
            consoleMock.Verify(c => c.SetPosition(It.IsAny<int>(), It.IsAny<int>()), Times.AtLeastOnce);
            consoleMock.Verify(c => c.SetTextColor(ConsoleColor.Yellow), Times.Once);
            consoleMock.Verify(c => c.Write($"{players[0].Name} Money: 3000£"), Times.Once);
            consoleMock.Verify(c => c.ResetColor(), Times.AtLeastOnce);

            // Ensure that the output contains the expected information
            string expectedOutput = $"Player 1 Money: 3000£Player 2 Money: 3000£";
            Assert.Equal(expectedOutput, output.ToString().Trim());

            // Clean up: Restore console output
            System.Console.SetOut(System.Console.Out);
        }

        [Fact]
        public void PrintColoredText_SetsColorPrintsTextAndResetsColor()
        {
            // Arrange
            var consoleMock = new Mock<IConsoleWrapper>();

            var squares = GenerateMockSquares();
            var rules = GenerateMockGameRules();

            var consolePrinter = new ConsolePrinter(consoleMock.Object, squares, rules);

            // Act
            consolePrinter.PrintColoredText("Test Text", ConsoleColor.Green);

            // Assert
            consoleMock.Verify(c => c.SetTextColor(ConsoleColor.Green), Times.Once);
            consoleMock.Verify(c => c.Write("Test Text"), Times.Once);
            consoleMock.Verify(c => c.ResetColor(), Times.Once);
        }

        [Fact]
        public void PrintText_ResetsColorSetsPositionAndPrintsText()
        {
            // Arrange
            var consoleMock = new Mock<IConsoleWrapper>();

            var squares = GenerateMockSquares();
            var rules = GenerateMockGameRules();

            var consolePrinter = new ConsolePrinter(consoleMock.Object, squares, rules);

            // Act
            consolePrinter.PrintText("Test Text");

            // Assert
            consoleMock.Verify(c => c.ResetColor(), Times.Once);
            consoleMock.Verify(c => c.SetPosition(It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            consoleMock.Verify(c => c.Write("Test Text"), Times.Once);
        }


        [Fact]
        public void PrintTextWaitForInput_ShouldPrintTextAndReadLine()
        {
            // Arrange
            var consoleMock = new Mock<IConsoleWrapper>();
            var output = new StringWriter();
            consoleMock.Setup(c => c.Write(It.IsAny<string>())).Callback<string>(s => output.Write(s));

            var squares = GenerateMockSquares();
            var rules = GenerateMockGameRules();

            var consolePrinter = new ConsolePrinter(consoleMock.Object, squares, rules);

            string text = "Test text";

            // Act
            consolePrinter.PrintTextWaitForInput(text);

            // Assert
            consoleMock.Verify(c => c.Write(text), Times.Once);
            consoleMock.Verify(c => c.ReadLine(), Times.Once);

            // Ensure that the output contains the expected information
            string expectedOutput = "Test text";
            Assert.Equal(expectedOutput, output.ToString().Trim());

            // Clean up: Restore console output
            System.Console.SetOut(System.Console.Out);
        }

        [Fact]
        public void EndPlayerTurnInfo_ShouldDisplayPlayerInfoAndPrintTextWaitForInput()
        {
            // Arrange
            var consoleMock = new Mock<IConsoleWrapper>();
            var output = new StringWriter();
            consoleMock.Setup(c => c.Write(It.IsAny<string>())).Callback<string>(s => output.Write(s));

            var squares = GenerateMockSquares();
            var rules = GenerateMockGameRules();

            var consolePrinter = new ConsolePrinter(consoleMock.Object, squares, rules);

            List<Player> players = GenerateMockPlayers();

            // Act
            consolePrinter.EndPlayerTurnInfo(players[0], players);

            // Assert
            consoleMock.Verify(c => c.Write(It.IsAny<string>()), Times.AtLeastOnce);
            consoleMock.Verify(c => c.ReadLine(), Times.Once);

            // Ensure that the output contains the expected information
            string expectedOutput = $"Player 1 Money: 3000£Player 2 Money: 3000£" +
                $"Player 1's Turn.\n Press Enter To End Turn";
            Assert.Equal(expectedOutput, output.ToString().Trim());

            // Clean up: Restore console output
            System.Console.SetOut(System.Console.Out);
        }

        [Fact]
        public void StartPlayerTurnInfo_ShouldDisplayPlayerInfoAndPrintTextWaitForInput()
        {
            // Arrange
            var consoleMock = new Mock<IConsoleWrapper>();
            var output = new StringWriter();
            consoleMock.Setup(c => c.Write(It.IsAny<string>())).Callback<string>(s => output.Write(s));

            var squares = GenerateMockSquares();
            var rules = GenerateMockGameRules();

            var consolePrinter = new ConsolePrinter(consoleMock.Object, squares, rules);

            List<Player> players = GenerateMockPlayers();

            // Act
            consolePrinter.StartPlayerTurnInfo(players[0], players);

            // Assert
            consoleMock.Verify(c => c.Write(It.IsAny<string>()), Times.AtLeastOnce);
            consoleMock.Verify(c => c.ReadLine(), Times.Once);

            // Ensure that the output contains the expected information
            string expectedOutput = $"Player 1 Money: 3000£Player 2 Money: 3000£" +
                $"Player 1's Turn.\n Press Enter To Continue";
            Assert.Equal(expectedOutput, output.ToString().Trim());

            // Clean up: Restore console output
            System.Console.SetOut(System.Console.Out);
        }
    }
}

