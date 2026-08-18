using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    public abstract class Square()
    {
        public int Position { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Info { get; set; } = string.Empty;
        public Player? Owner { get; set; }
        public int Price { get; set; }
        public int MortgageValue { get; set; }
        public bool IsMortgage { get; set; }

        public abstract void LandOn(Player player, Game game);
    }
}
