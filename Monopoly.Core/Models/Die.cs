using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models
{
    internal class Die : IDie
    { 
        private int DieSides {  get; set; }
        internal int Result {  get; set; }

        internal Die(int dieSides)
        {
            DieSides = dieSides;
        }

        private static Random _rnd = new Random();

        public void Roll()
        {
            Result = _rnd.Next(DieSides);
        }

        public int GetDieResult()
        {
            return Result;
        }

        public int GetDieType()
        {
            return DieSides;
        }
    }
}
