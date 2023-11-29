using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.Models
{
    internal class Street : Card
    {
        public int BoardPosition { get; set; }
        public int Price { get; set; }
        public int Mortgage { get; set; }
        public int GroupId { get; set; }
        public List<string> InfoList { get; set; }
    }
}
