using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.Models.Board
{
    internal abstract class SquareCard
    {
        public string Name { get; set; }
        public ConsoleColor BorderColor { get; set; }
        public int BoardPosition { get; set; }
        public string Info { get; set; }

    }
}
