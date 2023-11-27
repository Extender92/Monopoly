using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models
{
    internal class Player
    {
        public string Name { get; set; }
        public int Money { get; set; }
        public int Position { get; set; } = 0;

        public Player(string name)
        {
            Name = name;
            Money = 3000;
        }
    }
}
