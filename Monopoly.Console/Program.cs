namespace Monopoly.Console
{
    internal class Program
    {
        static void Main(string[] args)
        {
            List<string> infoLines = new List<string>();
            infoLines.Add("info");
            infoLines.Add("infoinfoinfo");
            infoLines.Add("infoinfoinfoinfoinfo");
            infoLines.Add("infoinfoinfo");
            infoLines.Add("På info");

            List<string> rents = new List<string>();
            rents.Add("rent");
            rents.Add("rent");
            rents.Add("rentrentrent");
            rents.Add("rentrentrent");
            rents.Add("På rent");

            List<string> info = new List<string>();

            int infoTextLength = infoLines.Select((line, i) => line.Length + rents[i].Length + 4).Max();

            string header = "Tågstation";

            int positionX = 5;
            int positionY = 5;

            int HorizontalSize = 30;
            int VerticalSize = 9;

            HorizontalSize = Math.Max(HorizontalSize, Math.Max(header.Length + 2, infoTextLength));

            for (int i = 0; i < infoLines.Count; i++)
            {
                int space = HorizontalSize - (infoLines[i].Length + rents[i].Length + 2);
                info.Add(infoLines[i] + ":".PadRight(space) + rents[i]);
                info.Add("hgfdhdfg");
                info.Add("hgfdhdfg");
            }

            header = Helpers.StringHelper.CenterString(header, HorizontalSize);

            GUI.Print.PrintCard(header, positionX, positionY, HorizontalSize, VerticalSize, info, ConsoleColor.Red, ConsoleColor.Blue);

            System.Console.ReadLine();




            info.Clear();

            for (int i = 0; i < infoLines.Count; i++)
            {
                int space = HorizontalSize - (infoLines[i].Length + rents[i].Length + 2);
                info.Add(rents[i] + ":".PadRight(space) + infoLines[i]);
            }

            GUI.Print.PrintCard(header, positionX, positionY, HorizontalSize, VerticalSize, info, ConsoleColor.Green, ConsoleColor.Yellow);

            System.Console.ReadLine();


            System.Console.Clear();
            info.Clear();

            string infoText = "Jätte lång text baserad på tågstationen för att testa hur koden hanterar radbrytningar!";

            HorizontalSize = 30;
            header = Helpers.StringHelper.CenterString(header, HorizontalSize);
            int length = HorizontalSize - 1;

            info = Helpers.StringHelper.CenterStringInList(Helpers.StringHelper.GetListOfStringsFromString(infoText, length), length);

            GUI.Print.PrintCard(header, positionX, positionY, HorizontalSize, VerticalSize, info, ConsoleColor.Green, ConsoleColor.Yellow);
        }
    }
}