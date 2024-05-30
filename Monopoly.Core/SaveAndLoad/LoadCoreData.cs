using Monopoly.Core.Interface;
using Monopoly.Core.Logs;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Monopoly.Core.Jail;

namespace Monopoly.Core.SaveAndLoad
{
    internal class LoadCoreData
    {
        internal static void LoadData(IGame game)
        {
            string filePath = "game_data.json";

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File {filePath} does not exist.");
            }

            try
            {
                string jsonData = File.ReadAllText(filePath);

                GameData gameData = JsonSerializer.Deserialize<GameData>(jsonData);

                var Rules = gameData.Rules;
                List<Player> Players = gameData.Players;
                Player CurrentPlayer = gameData.CurrentPlayer;



                List<int> SquarePosition = gameData.SquarePosition;
                List<int> SquarePlayerId = gameData.SquarePlayerId;
                List<int> SquareHouses = gameData.SquareHouses;


                var dice = new List<IDie>();
                for (int i = 0; i < Rules.NumberOfDice; i++)
                {
                    dice.Add(new Die(Rules.DieSides));
                }

                for (int i = 0; i < SquarePosition.Count; i++)
                {
                    Square square = game.Board.GetSquareAtPosition(SquarePosition[i]);
                    Player player = game.Players.FirstOrDefault(p => p.Id == SquarePlayerId[i]);
                    square.Owner = player;
                    if (square is PropertySquare property)
                    {
                        property.Houses = SquareHouses[i];
                    }
                }

                game.Rules = Rules;
                game.Players = Players;
                game.CurrentPlayer = CurrentPlayer;
                game.Fines = gameData.Fines;
                game.CurrentTurn = gameData.CurrentTurn;


                if (gameData.JailInfo != null)
                {
                    for (int i = 0; i < gameData.Players.Count; i++)
                    {
                        if (gameData.JailInfo[i] != null)
                        {
                            game.TheJail.PlayerGoToJail(game.Players[i]);
                            var info = game.TheJail.GetJailInfo(game.Players[i]);
                            info.TurnsInJail = gameData.JailInfo[i].TurnsInJail;
                        }
                    }
                }

                Console.WriteLine($"Data loaded from {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading data from {filePath}: {ex.Message}");
            }
        }
    }

    internal class GameData
    {
        public GameRules Rules { get; set; }
        public List<int> SquarePosition { get; set; }
        public List<int> SquarePlayerId { get; set; }
        public List<int> SquareHouses { get; set; }
        public int Fines { get; set; }
        public List<Player> Players { get; set; }
        public List<JailStatus> JailInfo { get; set; }
        public Player CurrentPlayer { get; set; }
        public int CurrentTurn { get; set; }
    }
}
