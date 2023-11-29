using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.Models
{
    internal class TablePiece
    {
        public int PlayerId { get; set; }
        public string Piece { get; set; }
        public ConsoleColor Color { get; set; }
    }
}
