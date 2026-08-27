using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Models.Board
{
    public class JailSquare : Square
    {
        public string InJailInfo { get; }
        public JailSquare(int position, string name, string info, string inJailInfo)
        {
            Position = position;
            Name = name;
            Info = info;
            InJailInfo = inJailInfo;
        }

        internal override void LandOn(Player player, Game game)
        {
            
        }
    }
}
