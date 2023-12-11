using Monopoly.Core.Events;
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
    internal class Game
    {
        internal GameHandler Handler {  get; set; }
        internal LogHandler Logs { get; set; }
        internal GameBoard Board { get; set; }
        internal List<Player> Players  { get; set;}
        internal List<IDie> Dice { get; set; }
        internal GameRules Rules { get; set; }
        internal Transaction Transactions { get; set; }
        internal Jail TheJail { get; set; }
        internal FortuneCardHandler FortuneCard { get; set; }
        internal int Fines { get; set; }

        public Game(
        List<Player> players,
        List<IDie> dice,
        GameRules rules)
        {
            Players = players;
            Dice = dice;
            Rules = rules;

            Fines = 0;
            Board = new GameBoard(rules);
            Logs = new LogHandler();
            FortuneCard = new FortuneCardHandler(rules);
            TheJail = new Jail(this);
            Handler = new GameHandler(this);
            Transactions = new Transaction(this);
        }
    }
}
