using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models
{
    public class Die : IDie
    {
        private int DieSides { get; set; }
        public int Result { get; private set; }

        public Die(int dieSides)
        {
            DieSides = dieSides;
        }

        private static Random _rnd = new Random();

        public void Roll()
        {
            Result = _rnd.Next(1, DieSides + 1);
        }

        public int GetDieResult()
        {
            return Result;
        }

        public int GetDieType()
        {
            return DieSides;
        }

        // Scramble Die For Jail
        public void ScrambleDie()
        {
            Result = -1;
        }
    }
}
