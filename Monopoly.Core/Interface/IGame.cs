using Monopoly.Core.Logs;
using Monopoly.Core.Notifications;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Models;
using Monopoly.Core.Presentation;
using Monopoly.Core.Randomness;
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
        DeckCollection Decks { get; }
        IReadOnlyList<Player> Players { get; }
        Player CurrentPlayer { get; }
        DiceRoll? LastDiceRoll { get; }
        GameRules Rules { get; }
        ProfilePresentation Presentation { get; }
        StatusCollection Statuses { get; }
        int Fines { get; }
        int CurrentTurn { get; }
        int ConsecutiveDoubles { get; }
        Player? Winner { get; }
        bool IsGameOver { get; }
        GamePhase Phase { get; }
        PendingDecision? PendingDecision { get; }

        void SetDecisionProvider(IPlayerDecisionProvider decisions);
        GameActionResult PlayTurn();
        GameActionResult SubmitDecision(DecisionResponse? response);
    }
}
