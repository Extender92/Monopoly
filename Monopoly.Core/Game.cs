using Monopoly.Core.Events;
using Monopoly.Core.Interface;
using Monopoly.Core.Logs;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core
{
    internal class Game : IGame
    {
        public GameHandler Handler { get; set; }
        public ILogHandler Logs { get; set; }
        public GameBoard Board { get; set; }
        public List<Player> Players { get; set; }
        public Player CurrentPlayer { get; set; }
        public List<IDie> Dice { get; set; }
        public GameRules Rules { get; set; }
        public Transaction Transactions { get; set; }
        public Jail TheJail { get; set; }
        public FortuneCardHandler FortuneCard { get; set; }
        public int Fines { get; set; }
        public int CurrentTurn { get; set; }

        public Game(
        List<Player> players,
        Player currentPlayer,
        List<IDie> dice,
        GameRules rules,
        ILogHandler logs)
        {
            Players = players;
            CurrentPlayer = currentPlayer;
            Dice = dice;
            Rules = rules;
            Logs = logs;

            Fines = 0;
            Board = new GameBoard(rules);
            FortuneCard = new FortuneCardHandler(rules);
            TheJail = new Jail(this, Board.Squares.First(s => s.Name == "Jail").Position);
            Handler = new GameHandler(this);
            Transactions = new Transaction(this);
        }

        public void NextPlayer()
        {
            int currentIndex = Players.IndexOf(CurrentPlayer);
            int nextIndex = (currentIndex + 1) % Players.Count;
            CurrentTurn = 1;
            CurrentPlayer = Players[nextIndex];
        }

        public void RemovePlayer(Player player)
        {
            if (Players.Contains(player))
            {
                if (CurrentPlayer == player) NextPlayer();

                Players.Remove(player);
            }
        }

        public void PlayerTakeTurn()
        {
            if (TheJail.IsPlayerInJail(CurrentPlayer))
            {
                PlayerTakeTurnInJail();
                return;
            }

            // Event for PlayerActionMenu.DisplayPlayerActionMainMenu();
            Square landedSquare = Board.GetSquareAtPosition(CurrentPlayer.Position);
            landedSquare.LandOn(CurrentPlayer, this);
            if (TheJail.IsPlayerInJail(CurrentPlayer)) return;

            CurrentTurn++;

            if (Handler.IsDiceDouble())
            {
                if (CurrentTurn > 3)
                {
                    TheJail.PlayerGoToJail(CurrentPlayer, "Rolled a Double more than 3 times in a row");
                    return;
                }
                Logs.CreateLog($"{CurrentPlayer.Name} rolled a double! Taking another turn.");
                PlayerTakeTurn();
            }
        }

        public void HandlePlayerInJail()
        {
            bool playerWantsToBuyOut = TheJail.TryPlayerBuyOut(CurrentPlayer);

            if (playerWantsToBuyOut)
            {
                string reason = TheJail.BuyOutPlayerFromJail(CurrentPlayer);
                TheJail.ReleasePlayerFromJail(CurrentPlayer, reason);
            }
        }

        public void PlayerTakeTurnInJail()
        {
            if (Handler.IsDiceDouble())
            {
                TheJail.ReleasePlayerFromJail(CurrentPlayer);
                int diceSum = Handler.CalculateDiceSum();
                CurrentPlayer.Position += diceSum;
                return;
            }

            TheJail.IncrementTurnsInJail(CurrentPlayer);

            if (TheJail.PlayerReachedMaxTurnsInJail(CurrentPlayer))
            {
                TheJail.HandleMaxTurnsInJail(CurrentPlayer);
            }
        }
    }
}
