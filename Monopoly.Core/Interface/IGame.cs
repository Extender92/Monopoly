using Monopoly.Core.Logs;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Interface
{
    internal interface IGame
    {
        GameHandler Handler { get; set; }
        ILogHandler Logs { get; set; }
        GameBoard Board { get; set; }
        List<Player> Players { get; set; }
        Player CurrentPlayer { get; set; }
        List<IDie> Dice { get; set; }
        GameRules Rules { get; set; }
        Transaction Transactions { get; set; }
        Jail TheJail { get; set; }
        FortuneCardHandler FortuneCard { get; set; }
        int Fines { get; set; }
        int CurrentTurn { get; set; }

        void NextPlayer();
        void RemovePlayer(Player player);
        void PlayerTakeTurn();
        void PlayerTakeTurnInJail();
    }
}
