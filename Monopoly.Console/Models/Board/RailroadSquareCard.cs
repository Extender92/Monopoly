using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.Models.Board
{
    internal class RailroadSquareCard : SquareCard
    {
        public List<string> Prop { get; set; } = new();
        public List<string> Rent { get; set; } = new();
    }
}
