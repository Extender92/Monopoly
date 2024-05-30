using Monopoly.Core.Interface;
using Monopoly.Core.Logs;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Monopoly.Core.Jail;

namespace Monopoly.Core.SaveAndLoad
{
    internal static class SaveCoreData
    {
        internal static void SaveData(IGame game)
        {
            List<JailStatus> jailStatuses = new List<JailStatus>();
            foreach (Player player in game.Players)
            {
                JailStatus info = game.TheJail.GetJailInfo(player);
                jailStatuses.Add(info);
            }

            List<Square> squares = game.Board.GetAllMortgageableSquares();

            List<int> SquarePosition = new List<int>();
            List<int> SquarePlayerId = new List<int>();
            List<int> SquareHouses = new List<int>();
            foreach (var square in squares)
            {
                if (square.Owner != null)
                {
                    SquarePosition.Add(square.Position);
                    SquarePlayerId.Add(square.Owner.Id);
                    if (square is PropertySquare property) SquareHouses.Add(property.Houses);
                    else SquareHouses.Add(0);
                }
            }

            string json = JsonSerializer.Serialize(new
            {
                SquarePosition = SquarePosition,
                SquarePlayerId = SquarePlayerId,
                SquareHouses = SquareHouses,
                Rules = game.Rules,
                Fines = game.Fines,
                Players = game.Players,
                JailInfo = jailStatuses,
                CurrentPlayer = game.CurrentPlayer,
                CurrentTurn = game.CurrentTurn
            });

            WriteDataToDrive(json);
        }

        internal static void WriteDataToDrive(string json)
        {
            string filePath = "game_data.json";

            // Write the JSON data to the file
            File.WriteAllText(filePath, json);

            Console.WriteLine($"Data saved to {filePath}");
            Console.ReadLine();
        }
    }
}
