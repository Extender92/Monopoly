using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    internal abstract class Square()
    {
        public int EventId { get; set; }
        public int Position { get; set; }
        public string Info { get; set; }
        public Player? Owner { get; set; }
        public abstract void LandOn(Player player);
    }
}
