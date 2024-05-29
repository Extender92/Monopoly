using Monopoly.Console.Models;
using Monopoly.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.GUI
{
    internal class Input
    {
        private IConsoleWrapper Console { get; set; }
        private IMenuOptionSelector Menu { get; set; }

        public Input(IConsoleWrapper consoleWrapper, IMenuOptionSelector menu)
        {
            Console = consoleWrapper;
            Menu = menu;
        }

        internal bool GetUserConfirmation()
        {
            List<string> menuChoices = Utilities.StringHelper.CreateStringList("Yes", "No");
            
            int index = Menu.GetSelectedOption(menuChoices, menuChoices.Max(s => s.Length));
            return index == 0; // True for yes and False for everything else
        }

        internal int GetNumberOfPlayers()
        {
            Console.Clear();
            Console.WriteLine("How many players?");
            List<string> menuChoices = Utilities.StringHelper.CreateStringList("2", "3", "4", "5", "6", "7", "8");
            int index = Menu.GetSelectedOption(menuChoices, menuChoices.Max(s => s.Length), 0, 3);
            Console.Clear();
            return index + 2;
        }
    }
}
