using Monopoly.Console.GUI;
using Monopoly.Console.Models;
using Monopoly.Core.Models;

namespace Monopoly.Console
{
    internal class Program
    {
        static void Main(string[] args)
        {


            //List<string> infoLines = new List<string>();
            //infoLines.Add("info");
            //infoLines.Add("infoagaininfo");
            //infoLines.Add("infoagaininfoagaininfo");
            //infoLines.Add("infoagaininfo");
            //infoLines.Add("På info");

            //List<string> rents = new List<string>();
            //rents.Add("rent");
            //rents.Add("rent");
            //rents.Add("rentagainrent");
            //rents.Add("rentagainrent");
            //rents.Add("På rent");

            //List<string> info = new List<string>();

            //int infoTextLength = infoLines.Select((line, i) => line.Length + rents[i].Length + 4).Max();

            //string header = "Trainstation";

            //int positionX = 5;
            //int positionY = 5;

            //int HorizontalSize = 30;
            //int VerticalSize = 9;

            //HorizontalSize = Math.Max(HorizontalSize, Math.Max(header.Length + 2, infoTextLength));

            //for (int i = 0; i < infoLines.Count; i++)
            //{
            //    int space = HorizontalSize - (infoLines[i].Length + rents[i].Length + 2);
            //    info.Add(infoLines[i] + ":".PadRight(space) + rents[i]);
            //    info.Add("howdoyou");
            //    info.Add("howdoyou");
            //}

            //header = Helpers.StringHelper.CenterString(header, HorizontalSize);

            //GUI.Print.PrintCard(header, positionX, positionY, HorizontalSize, VerticalSize, info, ConsoleColor.Red, ConsoleColor.Blue);

            //System.Console.ReadLine();




            //info.Clear();

            //for (int i = 0; i < infoLines.Count; i++)
            //{
            //    int space = HorizontalSize - (infoLines[i].Length + rents[i].Length + 2);
            //    info.Add(rents[i] + ":".PadRight(space) + infoLines[i]);
            //}

            //GUI.Print.PrintCard(header, positionX, positionY, HorizontalSize, VerticalSize, info, ConsoleColor.Green, ConsoleColor.Yellow);

            //System.Console.ReadLine();


            //System.Console.Clear();
            //info.Clear();

            //string infoText = "Jätte lång text Based på Trainstation för att testa how koden handle radbrytningar!";

            //HorizontalSize = 30;
            //header = Helpers.StringHelper.CenterString(header, HorizontalSize);
            //int length = HorizontalSize - 1;

            //info = Helpers.StringHelper.CenterStringInList(Helpers.StringHelper.GetListOfStringsFromString(infoText, length), length);

            //GUI.Print.PrintCard(header, positionX, positionY, HorizontalSize, VerticalSize, info, ConsoleColor.Green, ConsoleColor.Yellow);




            Run();

        }

        private static void Run()
        {
            int numberOfPlayers = 2;
            int numberOfDice = 2;
            int dieSides = 6;
            
            Core.Game Game = Core.GameSetup.Setup(numberOfPlayers, numberOfDice, dieSides);

            List<TablePiece> tablePieces = new List<TablePiece>();
            foreach (Player player in Game.Players) 
            {
                tablePieces.Add(ChooseTablePiece(player.Id));
            }

            System.Console.Clear();
            while (true)
            {
                foreach (var player in Game.Players)
                {
                    Print.PrintBoard(Game.Players, tablePieces);
                    System.Console.SetCursorPosition(0, 0);
                    System.Console.WriteLine("Press Enter");
                    System.Console.ReadLine();
                    Game.PlayerTurn(player);
                    System.Console.Clear();
                }
            }
        }

        private static TablePiece ChooseTablePiece(int playerId)
        {
            TablePiece tablePiece = new TablePiece();

            tablePiece.PlayerId = playerId;

            System.Console.Write("Enter a key: ");
            tablePiece.Piece = System.Console.ReadKey().Key.ToString();

            System.Console.WriteLine("\nYou entered: " + tablePiece.Piece);
            tablePiece.Color = ConsoleColor.Green;
            return tablePiece;
        }

    

    }
}