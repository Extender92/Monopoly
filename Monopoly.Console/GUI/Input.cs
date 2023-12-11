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
        internal static bool GetUserConfirmation()
        {
            List<string> menuChoices = Helpers.StringHelper.CreateStringList("Yes", "No");
            
            int index = MenuOptionSelector.GetSelectedOption(menuChoices, menuChoices.Max(s => s.Length));
            return index == 0; // True for yes and False for everything else
        }
    }
}
