using Monopoly.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core
{
    internal class GameRules
    {
        public int NumberOfDice { get; set; }
        public int DieSides { get; set; }
        public GameRules(int numberOfDice, int dieSides)
        {
            NumberOfDice = numberOfDice;
        }
    }
}
