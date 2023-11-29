using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.Models
{
    internal class DrawCards : Card
    {
        public int Id { get; set; }
        public string Info { get; set; }
    }
}
