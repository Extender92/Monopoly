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
        private static IConsoleWrapper Console = new ConsoleWrapper();

        internal static bool GetUserConfirmation()
        {
            List<string> menuChoices = Helpers.StringHelper.CreateStringList("Yes", "No");
            
            int index = MenuOptionSelector.GetSelectedOption(menuChoices, menuChoices.Max(s => s.Length));
            return index == 0; // True for yes and False for everything else
        }

        internal static int GetNumberOfPlayers()
        {
            Console.Clear();
            Console.WriteLine("How many players?");
            List<string> menuChoices = Helpers.StringHelper.CreateStringList("2", "3", "4", "5", "6", "7", "8");
            int index = MenuOptionSelector.GetSelectedOption(menuChoices, menuChoices.Max(s => s.Length), 0, 3);
            Console.Clear();
            return index + 2;
        }
    }
}
