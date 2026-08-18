using Monopoly.Core.Events;
using Monopoly.Core.Interface;
using Monopoly.Core.Logs;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;

namespace Monopoly.Core;

public class Game : IGame
{
    public GameHandler Handler { get; set; }
    public ILogHandler Logs { get; set; }
    public GameBoard Board { get; set; }
    public List<Player> Players { get; set; }
    public Player CurrentPlayer { get; set; }
    public List<IDie> Dice { get; set; }
    public GameRules Rules { get; set; }
    public Transaction Transactions { get; set; }
    public Jail TheJail { get; set; }
    public FortuneCardHandler FortuneCard { get; set; }
    public IPlayerDecisionProvider Decisions { get; set; }
    public int Fines { get; set; }
    public int CurrentTurn { get; set; }
    public int ConsecutiveDoubles { get; private set; }
    public Player? Winner { get; internal set; }
    public bool IsGameOver => Winner is not null || Players.Count(p => !p.IsBankrupt) <= 1;

    internal void RestoreConsecutiveDoubles(int value) => ConsecutiveDoubles = value;

    internal void RestoreWinner(Player? winner) => Winner = winner;

    public Game(
        List<Player> players,
        Player currentPlayer,
        List<IDie> dice,
        GameRules rules,
        ILogHandler logs,
        IPlayerDecisionProvider? decisions = null)
    {
        Players = players;
        CurrentPlayer = currentPlayer;
        Dice = dice;
        Rules = rules;
        Logs = logs;
        Decisions = decisions ?? new DefaultPlayerDecisionProvider();

        Fines = 0;
        CurrentTurn = 1;
        Board = new GameBoard(rules);
        FortuneCard = new FortuneCardHandler(rules);
        TheJail = new Jail(this, Board.Squares.First(s => s.Name == "Jail").Position);
        Handler = new GameHandler(this);
        Transactions = new Transaction(this);

        if (Logs is LogHandler logHandler)
            logHandler.OwnerGame = this;
    }

    /// <summary>Executes one complete dice roll.</summary>
    public TurnResult PlayTurn()
    {
        if (IsGameOver)
        {
            Winner ??= Players.FirstOrDefault(p => !p.IsBankrupt);
            return new TurnResult { Player = CurrentPlayer, GameOver = true, Winner = Winner };
        }

        Player player = CurrentPlayer;
        if (player.IsBankrupt)
        {
            AdvanceToNextActivePlayer();
            return new TurnResult { Player = player, PlayerBankrupt = true, GameOver = IsGameOver, Winner = Winner };
        }

        if (TheJail.IsPlayerInJail(player))
            return PlayJailTurn(player);

        Handler.RollDice(player);
        IReadOnlyList<int> results = Dice.Select(die => die.GetDieResult()).ToList();
        int diceSum = Handler.CalculateDiceSum();
        bool isDouble = Handler.IsDiceDouble();

        if (isDouble && ConsecutiveDoubles == 2)
        {
            ConsecutiveDoubles = 0;
            TheJail.PlayerGoToJail(player, "Rolled doubles three times in a row");
            AdvanceToNextActivePlayer();
            return BuildResult(player, results, diceSum, null, true, true, false, false);
        }

        Square landedSquare = MovePlayerBySteps(player, diceSum);
        GameEvents.InvokeLandOnSquare(this, landedSquare);
        landedSquare.LandOn(player, this);

        bool bankrupt = player.IsBankrupt;
        bool sentToJail = TheJail.IsPlayerInJail(player);
        if (bankrupt || sentToJail || !isDouble)
        {
            ConsecutiveDoubles = 0;
            AdvanceToNextActivePlayer();
        }
        else
        {
            ConsecutiveDoubles++;
            CurrentTurn++;
        }

        return BuildResult(player, results, diceSum, landedSquare, isDouble, sentToJail, false, isDouble && !bankrupt && !sentToJail);
    }

    private TurnResult PlayJailTurn(Player player)
    {
        if (Decisions.ConfirmJailBuyout(player))
        {
            if (player.NumberOfGetOutOFJailCards > 0)
            {
                TheJail.BuyOutPlayerFromJail(player);
                TheJail.ReleasePlayerFromJail(player, ", used a Get Out of Jail For Free card");
            }
            else if (Handler.TryResolvePayment(player, Rules.JailFine, null, "Could not afford to pay Jail Fine"))
            {
                TheJail.ReleasePlayerFromJail(player, ", paid the fine to get out of jail");
            }

            if (player.IsBankrupt)
            {
                AdvanceToNextActivePlayer();
                return BuildResult(player, Array.Empty<int>(), 0, null, false, false, false, false, true);
            }
        }

        Handler.RollDice(player);
        IReadOnlyList<int> results = Dice.Select(die => die.GetDieResult()).ToList();
        int diceSum = Handler.CalculateDiceSum();
        bool isDouble = Handler.IsDiceDouble();

        if (isDouble)
        {
            TheJail.ReleasePlayerFromJail(player, ", rolled doubles");
            Square landedSquare = MovePlayerBySteps(player, diceSum);
            GameEvents.InvokeLandOnSquare(this, landedSquare);
            landedSquare.LandOn(player, this);
            ConsecutiveDoubles = 0;
            AdvanceToNextActivePlayer();
            return BuildResult(player, results, diceSum, landedSquare, true, false, true, false);
        }

        TheJail.IncrementTurnsInJail(player);
        if (TheJail.PlayerReachedMaxTurnsInJail(player))
        {
            if (player.NumberOfGetOutOFJailCards > 0)
            {
                TheJail.BuyOutPlayerFromJail(player);
                TheJail.ReleasePlayerFromJail(player, ", used a Get Out of Jail For Free card");
            }
            else if (Handler.TryResolvePayment(player, Rules.JailFine, null, "Could not afford to pay Jail Fine"))
            {
                TheJail.ReleasePlayerFromJail(player, ", paid the fine to get out of jail");
            }
            else
            {
                AdvanceToNextActivePlayer();
                return BuildResult(player, results, diceSum, null, false, false, false, false, true);
            }
        }

        AdvanceToNextActivePlayer();
        return BuildResult(player, results, diceSum, null, false, false, false, false);
    }

    private TurnResult BuildResult(
        Player player,
        IReadOnlyList<int> results,
        int diceSum,
        Square? landedSquare,
        bool wasDouble,
        bool wasSentToJail,
        bool wasReleasedFromJailByDouble,
        bool extraTurn,
        bool playerBankrupt = false)
    {
        return new TurnResult
        {
            Player = player,
            DiceResults = results,
            DiceSum = diceSum,
            LandedSquare = landedSquare,
            WasDouble = wasDouble,
            WasSentToJail = wasSentToJail,
            WasReleasedFromJailByDouble = wasReleasedFromJailByDouble,
            ExtraTurn = extraTurn,
            PlayerBankrupt = playerBankrupt || player.IsBankrupt,
            GameOver = IsGameOver,
            Winner = Winner
        };
    }

    public Square MovePlayerBySteps(Player player, int steps)
    {
        Handler.MovePlayerAndInvokeEvent(player, player.Position + steps);
        return Board.GetSquareAtPosition(player.Position);
    }

    public void NextPlayer() => AdvanceToNextActivePlayer();

    private void AdvanceToNextActivePlayer()
    {
        if (Players.Count == 0)
        {
            Winner = null;
            return;
        }

        List<Player> activePlayers = Players.Where(p => !p.IsBankrupt).ToList();
        if (activePlayers.Count <= 1)
        {
            Winner = activePlayers.SingleOrDefault();
            return;
        }

        int currentIndex = Players.IndexOf(CurrentPlayer);
        if (currentIndex < 0) currentIndex = -1;

        for (int offset = 1; offset <= Players.Count; offset++)
        {
            Player candidate = Players[(currentIndex + offset + Players.Count) % Players.Count];
            if (!candidate.IsBankrupt)
            {
                CurrentPlayer = candidate;
                CurrentTurn = 1;
                ConsecutiveDoubles = 0;
                return;
            }
        }
    }

    public void RemovePlayer(Player player)
    {
        if (!Players.Remove(player)) return;
        if (Players.Count == 0)
        {
            Winner = null;
            return;
        }

        if (CurrentPlayer == player)
            AdvanceToNextActivePlayer();
        else if (Players.Count(p => !p.IsBankrupt) <= 1)
            AdvanceToNextActivePlayer();
    }

}
