
namespace Monopoly.Core.Models
{
    internal class ParkingSpace : Square
    {
        public ParkingSpace()
        {
            Position = 20;
            Info = "Free Parking";
        }

        internal void GetFreeParkingSpace()
        {
            Console.WriteLine($"You are {Info}");
        }
    }
}