using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.Utilities
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
            // Split string every maxLength or every first . after letters
            // "letter..test" leaves [0]"letter." [1]"test"

            const int startIndex = 0;

            List<string> list = new List<string>();

            if (string.IsNullOrEmpty(text))
            {
                return list;
            }

            while (text.Length > maxLength)
            {
                int lastSpaceIndex = text.LastIndexOf(' ', maxLength);
                int dotIndex = text.IndexOf('.', startIndex, maxLength);

                // Check for consecutive dots
                if (dotIndex != -1 && dotIndex + 1 < text.Length && text[dotIndex + 1] == '.')
                {
                    // Include the first dot if two dots in row to the current string
                    dotIndex++;
                }

                int splitIndex = (dotIndex != -1) ? dotIndex : lastSpaceIndex;

                list.Add(text.Substring(0, splitIndex).Trim());
                text = text.Substring(splitIndex + 1).TrimStart();
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