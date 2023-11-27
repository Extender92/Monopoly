using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models
{
    internal class Railroad : Square
    {
        public int Price { get; set; }
        public int BaseRent {  get; set; }
    }
}
