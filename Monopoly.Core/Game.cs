using Monopoly.Core.Events;
using Monopoly.Core.Interface;
using Monopoly.Core.Logs;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
        public List<IDie> Dice { get; set; }
        public GameRules Rules { get; set; }
        public Transaction Transactions { get; set; }
        public Jail TheJail { get; set; }
        public FortuneCardHandler FortuneCard { get; set; }
        public int Fines { get; set; }

        public Game(
        List<Player> players,
        List<IDie> dice,
        GameRules rules,
        ILogHandler logs)
        {
            Players = players;
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

        public void PlayerTakeTurnInJail(Player player)
        {
            bool playerWantsToBuyOut = TheJail.TryPlayerBuyOut(player);

            if (playerWantsToBuyOut)
            {
                string reason = TheJail.BuyOutPlayerFromJail(player);
                Handler.RollDice(player);
                TheJail.ReleasePlayerFromJail(player, reason);
                return;
            }

            Handler.RollDice(player);

            if (Handler.IsDiceDouble())
            {
                TheJail.ReleasePlayerFromJail(player);
                return;
            }

            TheJail.IncrementTurnsInJail(player);

            if (TheJail.PlayerReachedMaxTurnsInJail(player))
            {
                TheJail.HandleMaxTurnsInJail(player);
            }
        }
    }
}
