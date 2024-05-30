using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.Utilities
{
    internal static class EnumExtensions
    {
        internal static string GetDisplayName(this Enum value)
        {
            var type = value.GetType();
            var member = type.GetMember(value.ToString()).FirstOrDefault();
            var attribute = member?.GetCustomAttribute<DisplayNameAttribute>();

            return attribute?.DisplayName ?? value.ToString();
        }
    }
}
