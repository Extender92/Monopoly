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
    internal static class Game
    {
        public static GameHandler Handler = new GameHandler();
        internal static LogHandler Logs { get; set; } = new LogHandler();
        internal static GameBoard Board { get; set; }
        internal static List<Player> Players  { get; set;}
        internal static List<IDie> Dice { get; set; }
        internal static GameRules Rules { get; set; }
        internal static Transaction Transactions { get; set; }
        internal static Jail TheJail { get; set; }
        internal static FortuneCardHandler FortuneCard { get; set; }
        internal static int Fines { get; set; }

     
    }
}
