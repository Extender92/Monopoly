using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monopoly.Core.Models.Board;

namespace Monopoly.Core.Models
{
    internal class Player(string name, int id)
    {
        public int Id { get; set; } = id;
        public string Name { get; set; } = name;
        public int Money { get; set; } = 3000;
        public int Position { get; set; } = 0;
        public int NumberOfGetOutOFJailCards { get; set; } = 0;
    }
}
