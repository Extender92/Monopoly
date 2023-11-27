using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models
{
    public class Die
    {
        public int DiceOne {  get; set; }
        public int DiceTwo{  get; set; }

        private static Random _rnd = new Random();
        public int Roll()
        {
            DiceOne = _rnd.Next(1,6);
            DiceTwo = _rnd.Next(1,6);

            return (DiceOne + DiceTwo);
        }

        public bool IsDouble()
        {
            return DiceOne == DiceTwo;
        }

    }
}
