using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.GUI
{
    internal class ConsolePositions
    {
        public static int BoardPosX { get; set; }
        public static int BoardPosY { get; set; }
        public static int LogPosX { get; set; }
        public static int LogPosY { get; set; }
        public static int TextPosX { get; set; }
        public static int TextPosY { get; set; }
        public static int CardPosX { get; set; }
        public static int CardPosY { get; set; }
        public static int MenuPosX { get; set; }
        public static int MenuPosY { get; set; }
        public static int ListMenuPosX { get; set; }
        public static int ListMenuPosY { get; set; }
        public static int PlayerInformationX { get; set; }
        public static int PlayerInformationY { get; set; }

        public ConsolePositions()
        {
            SetStandardPositions();
        }

        internal static void SetStandardPositions()
        {
            BoardPosX = 2;
            BoardPosY = 6;
            LogPosX = 2;
            LogPosY = 18;
            TextPosX = 1;
            TextPosY = 0;
            CardPosX = 82;
            CardPosY = 1;
            MenuPosX = 10;
            MenuPosY = 2;
            ListMenuPosX = 10;
            ListMenuPosY = 2;
            PlayerInformationX = BoardPosX + 6;
            PlayerInformationY = BoardPosY + 2;
        }

        internal static void SetGameBoardMenuPositions()
        {
            MenuPosX = 2;
            MenuPosY = 1;
        }
    }
}
