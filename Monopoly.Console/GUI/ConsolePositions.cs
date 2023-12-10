using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.GUI
{
    internal class ConsolePositions
    {
        public int BoardXPos { get; set; }
        public int BoardYPos { get; set; }
        public int LogPosX { get; set; }
        public int LogPosY { get; set; }
        public int TextPosX { get; set; }
        public int TextPosY { get; set; }
        public int CardPosX { get; set; }
        public int CardPosY { get; set; }

        public ConsolePositions()
        {
            SetStandardPositions();
        }

        internal void SetStandardPositions()
        {
            BoardXPos = 2;
            BoardYPos = 6;
            LogPosX = 2;
            LogPosY = 15;
            TextPosX = 1;
            TextPosY = 0;
            CardPosX = 15;
            CardPosY = 3;
        }
    }
}
