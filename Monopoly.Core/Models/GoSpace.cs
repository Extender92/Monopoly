namespace Monopoly.Core.Models
{
    internal class GoSpace : Square
    {
        public GoSpace()
        {
            Position = 0;
            Info = "Go";
        }

        internal void GetGoSpace()
        {
            Console.WriteLine($"You are in {Info}");
        }
    }
}