using Monopoly.Core.Data;
using Monopoly.Core.Models;

namespace Monopoly.Core
{
    internal class Program
    {
        static void Main(string[] args)
        {
            List<Player> players = new List<Player>
            {
                new Player("Player 1",1),
                new Player("Player 2", 2)
            };

            var streetCards = CardSet.GetStreetCards();

            while (true)
            {
                PlayGame(players, streetCards);
                Console.ReadKey();
                Console.Clear();
            }




            //List<Square> squares = new List<Square>();


            //for (int i = 0; i < 40; i++)
            //{
            //    if (i == 0)
            //    {
            //        squares.Add(new GoSpace());
            //    }
            //    else if (i  == 4 )
            //    {
            //        squares.Add(new Tax(4));

            //    }        
            //    else if (i == 38)
            //    {
            //        squares.Add(new Tax(38));
            //    }
            //    else if (i == 10)
            //    {
            //        squares.Add(new Jail());
            //    }
            //    else if (i == 20)
            //    {
            //        squares.Add(new ParkingSpace());
            //    }
            //    else if (i == 30)
            //    {
            //        squares.Add(new JailSpace());
            //    }
            //    else if(i == 5 )
            //    {
            //        squares.Add(new Railroad(5));
            //    }                
            //    else if(i == 15)
            //    {

            //        squares.Add(new Railroad(15));

            //    }               
            //    else if(i == 25)
            //    {

            //        squares.Add(new Railroad(25));
            //    }                
            //    else if(i == 35)
            //    {
            //        squares.Add(new Railroad(35));
            //    }                
            //    else if(i == 5 || i == 15 || i == 25 || i == 35)
            //    {
            //        squares.Add(new Railroad(5));
            //        squares.Add(new Railroad(15));
            //        squares.Add(new Railroad(25));
            //        squares.Add(new Railroad(35));
            //    }
            //    else
            //    {
            //        squares.Add(new Square() { Position = i , Info = "Street"});
            //    }
            //}
            //foreach (var square in squares)
            //{
            //    Console.WriteLine($"{square.Position}: {square.Info}");
            //}

        }

        public static void PlayGame(List<Player> players, List<Street> streetCards)
        {
            // Simple game loop
            foreach (Player player in players)
            {
                Random random = new Random();
                Console.WriteLine($"Player {player.Name}'s turn.");
                Console.WriteLine($"Current Money: {player.Money}");

                // Simulate player moving on the board
                player.Position = (player.Position + random.Next(0,6)) % streetCards.Count;

                Street currentStreet = streetCards[player.Position];
                Console.WriteLine($"Landed on {currentStreet.Name}");

                if (currentStreet.Owner == null)
                {
                    Console.WriteLine($"Do you want to buy {currentStreet.Name} for {currentStreet.Price}? (Y/N)");
                    string input = Console.ReadLine();
                    if (input.ToUpper() == "Y")
                    {
                        player.Buy(currentStreet);
                        Console.WriteLine($"{currentStreet.Name} is now owned by {player.Name}");
                    }
                }
                else if (currentStreet.Owner != player)
                {
                    Console.WriteLine($"You need to pay rent to {currentStreet.Owner.Name}.");
                    player.PayRent(currentStreet);
                    Console.WriteLine($"Remaining Money: {player.Money}");
                }
                else
                {
                    Console.WriteLine($"Do you want to sell {currentStreet.Name}? (Y/N)");
                    string input = Console.ReadLine();
                    if (input.ToUpper() == "Y")
                    {
                        player.Sell(currentStreet);
                        Console.WriteLine($"{currentStreet.Name} is no longer owned by {player.Name}");
                    }
                }

                Console.WriteLine("Press Enter to continue to the next turn...");
                Console.ReadLine();
            }
        }
    }
}
