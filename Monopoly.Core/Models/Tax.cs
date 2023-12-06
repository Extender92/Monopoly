using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models
{
    internal class Tax : Square
    {
        public Tax(int position)
        {
            Position = position;
            Info = "Tax";
        }

        internal void GetTax()
        {
            Console.WriteLine($"You muste pay {Info}");
        }
    }
}
