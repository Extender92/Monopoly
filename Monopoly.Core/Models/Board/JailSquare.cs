using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    internal class JailSquare : Square
    {
        public string InJailInfo {  get; set; }
        public JailSquare(int position, string name, string info, string inJailInfo)
        {
            Position = position;
            Name = name;
            Info = info;
            InJailInfo = inJailInfo;
        }

        public override void LandOn(Player player)
        {
            
        }
    }
}
