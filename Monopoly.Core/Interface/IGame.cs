using Monopoly.Core.Logs;
using Monopoly.Core.Notifications;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Core.Interface
{
    public interface IGame
    {
        IGameLog Logs { get; }
        IGameNotificationSource Notifications { get; }
        GameBoard Board { get; }
        IReadOnlyList<Player> Players { get; }
        Player CurrentPlayer { get; }
        IReadOnlyList<IDieView> Dice { get; }
        GameRules Rules { get; }
        Jail TheJail { get; }
        FortuneCardHandler FortuneCard { get; }
        int Fines { get; }
        int CurrentTurn { get; }
        int ConsecutiveDoubles { get; }
        Player? Winner { get; }
        bool IsGameOver { get; }
        GamePhase Phase { get; }
        PendingDecision? PendingDecision { get; }

        void SetDecisionProvider(IPlayerDecisionProvider decisions);
        bool TryBuyHouse(Player player, PropertySquare property);
        bool TrySellHouse(Player player, PropertySquare property);
        bool TryMortgageProperty(Player player, Square square);
        bool TryRepayMortgage(Player player, Square square);
        GameActionResult PlayTurn();
        GameActionResult SubmitDecision(DecisionResponse? response);
    }
}
