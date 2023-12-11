using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.GUI
{
    internal class ConsolePositions
    {
        public int BoardPosX { get; set; }
        public int BoardPosY { get; set; }
        public int LogPosX { get; set; }
        public int LogPosY { get; set; }
        public int TextPosX { get; set; }
        public int TextPosY { get; set; }
        public int CardPosX { get; set; }
        public int CardPosY { get; set; }
        public int MenuPosX { get; set; }
        public int MenuPosY { get; set; }
        public int PlayerInformationX { get; set; }
        public int PlayerInformationY { get; set; }

        public ConsolePositions()
        {
            SetStandardPositions();
        }

        internal void SetStandardPositions()
        {
            BoardPosX = 2;
            BoardPosY = 6;
            LogPosX = 2;
            LogPosY = 18;
            TextPosX = 1;
            TextPosY = 0;
            CardPosX = 82;
            CardPosY = 5;
            MenuPosX = 10;
            MenuPosY = 2;
            PlayerInformationX = 74;
            PlayerInformationY = 0;
        }
    }
}
