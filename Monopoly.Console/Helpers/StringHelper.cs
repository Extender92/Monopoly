using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.Helpers
{
    internal static class StringHelper
    {
        internal static string CenterString(this string stringToCenter, int totalLength)
        {
            if (string.IsNullOrEmpty(stringToCenter))
            {
                return stringToCenter;
            }

            stringToCenter = stringToCenter.Trim();

            return stringToCenter.PadLeft(((totalLength - stringToCenter.Length) / 2)
                                + stringToCenter.Length)
                                .PadRight(totalLength);
        }

        internal static List<string> CenterStringInList(List<string> list, int totalLength)
        {
            return list.Select(str => CenterString(str, totalLength)).ToList();
        }

        internal static List<string> GetListOfStringsFromString(string text, int maxLength)
        {
            List<string> list = new List<string>();

            if (string.IsNullOrEmpty(text))
            {
                return list;
            }

            while (text.Length > maxLength)
            {
                int lastSpaceIndex = text.LastIndexOf(' ', maxLength);

                if (lastSpaceIndex == -1)
                {
                    list.Add(text.Substring(0, maxLength));
                    text = text.Substring(maxLength).TrimStart();
                }
                else
                {
                    list.Add(text.Substring(0, lastSpaceIndex).Trim());
                    text = text.Substring(lastSpaceIndex + 1).TrimStart();
                }
            }

            list.Add(text.Trim());

            return list;
        }

        internal static List<string> CreateStringList(params string[] strings)
        {
            if (strings == null || strings.Length == 0) return new List<string>();
            return new List<string>(strings);
        }
    }
}
