using Monopoly.Console.Builder;
using Monopoly.Console.Models.Board;
using Monopoly.Core;
using Monopoly.Core.Models.Board;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.GUI
{
    internal class ConsoleCardPrinter
    {
        internal IConsoleWrapper Console { get; set; }

        private List<SquareCard> SquareCards;
        private int CardPosX { get; set; }
        private int CardPosY { get; set; }

        public ConsoleCardPrinter(IConsoleWrapper consoleWrapper, List<Square> squares, GameRules gameRules)
        {
            Console = consoleWrapper;
            SetCardPosition();
            GetAllSquareCards(squares, gameRules);
        }

        internal void SetCardPosition()
        {
            CardPosX = ConsolePositions.CardPosX;
            CardPosY = ConsolePositions.CardPosY;
        }

        internal void GetAllSquareCards(List<Square> squares, GameRules gameRules)
        {
            SquareCards = SquareCardBuilder.BuildAllSquareCards(squares, gameRules);
        }

        internal void PrintCard(string header, int width, int maxInfoVerticalLength, List<string> info, ConsoleColor borderColor = ConsoleColor.White, ConsoleColor textColor = ConsoleColor.White)
        {
            // Header Text
            // Row two
            Console.SetTextColor(textColor);
            Console.SetPosition(CardPosX + 1, CardPosY + 1);
            Console.Write(header);

            // Header Border
            // Row one
            Console.SetTextColor(borderColor);
            Console.SetPosition(CardPosX, CardPosY);
            Console.Write('┌' + new String('─', width) + '┐');

            // Header Border
            // Row two
            Console.SetPosition(CardPosX, CardPosY + 1);
            Console.Write("│");
            Console.SetPosition((CardPosX + width + 1), CardPosY + 1);
            Console.Write("│");

            // Header Border
            // Row Three
            Console.SetPosition(CardPosX, CardPosY + 2);
            Console.Write('├' + new String('─', width) + '┤');

            // Body
            // For each row in Y length
            for (int i = 0; i <= maxInfoVerticalLength; i++)
            {
                Console.SetPosition(CardPosX, CardPosY + 3 + i);
                Console.Write("│");

                Console.SetTextColor(textColor);
                Console.Write(i >= info.Count ?
                    new String(' ', width) :
                    " " + info[i] + " ");

                Console.SetTextColor(borderColor);
                Console.SetPosition((CardPosX + width + 1), CardPosY + 3 + i);
                Console.Write("│");
            }

            // Footer
            Console.SetPosition(CardPosX, CardPosY + (maxInfoVerticalLength + 3));
            Console.Write('└' + new String('─', width) + '┘');
            Console.ResetColor();
        }

        // Needs refactoring and Testing
        internal void PrepareAndPrintSquareCard(int boardPosition)
        {
            SquareCard squareCard = SquareCards.First(s => s.BoardPosition == boardPosition);

            int cardHorizontalLength = 30;
            int maxInfoVerticalLength = 9;

            var borderColor = ConsoleColor.White;

            List<string> infoLines = new List<string>();
            List<string> rents = new List<string>();
            int infoTextLength = 0;

            if (squareCard is PropertySquareCard propertySquareCard)
            {
                borderColor = propertySquareCard.BorderColor;
                infoLines.AddRange(propertySquareCard.Prop);
                rents.AddRange(propertySquareCard.Rent);
                infoTextLength = infoLines.Select((line, i) => line.Length + rents[i].Length + 4).Max();
            }
            else if (squareCard is RailroadSquareCard railroadSquareCard)
            {
                infoLines.AddRange(railroadSquareCard.Prop);
                rents.AddRange(railroadSquareCard.Rent);
                infoTextLength = infoLines.Select((line, i) => line.Length + rents[i].Length + 4).Max();
            }


            string header = squareCard.Name;

            cardHorizontalLength = Math.Max(cardHorizontalLength, Math.Max(header.Length + 2, infoTextLength));

            List<string> info = new List<string>();

            for (int i = 0; i < infoLines.Count; i++)
            {
                int space = cardHorizontalLength - (infoLines[i].Length + rents[i].Length + 2);
                info.Add($"{infoLines[i]}:{new string(' ', space)}{rents[i]}");
            }
            info.Add("");

            string infoText = squareCard.Info;
            int length = cardHorizontalLength - 1;

            List<string> stringList = Utilities.StringHelper.CenterStringInList(Utilities.StringHelper.GetListOfStringsFromString(infoText, length), length);
            info.AddRange(stringList);
            header = Utilities.StringHelper.CenterString(header, cardHorizontalLength);

            if (maxInfoVerticalLength < info.Count) maxInfoVerticalLength = info.Count;

            PrintCard(header, cardHorizontalLength, maxInfoVerticalLength, info, borderColor);
        }
    }
}
