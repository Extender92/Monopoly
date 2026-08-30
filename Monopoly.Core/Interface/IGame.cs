using Monopoly.Core.Logs;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Notifications;
using Monopoly.Core.Presentation;
using Monopoly.Core.Randomness;

namespace Monopoly.Core.Interface;

public interface IGame
{
    IGameLog Logs { get; }
    IGameNotificationSource Notifications { get; }
    GameBoard Board { get; }
    DeckCollection Decks { get; }
    IReadOnlyList<Player> Players { get; }
    Player CurrentPlayer { get; }
    DiceRoll? LastDiceRoll { get; }
    ProfilePresentation Presentation { get; }
    ValidatedGameProfile Profile { get; }
    StatusCollection Statuses { get; }
    OwnershipCollection Ownership { get; }
    ProfileModuleState ModuleState { get; }
    int RoundNumber { get; }
    Player? Winner { get; }
    bool IsGameOver { get; }
    GamePhase Phase { get; }
    PendingDecision? PendingDecision { get; }

    GameActionResult PlayTurn();
    GameActionResult SubmitDecision(DecisionResponse? response);
}
