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
        public int Price { get; set; }
        public int Mortgage { get; set; }
        public List<string> InfoList { get; set; }

    }
}
