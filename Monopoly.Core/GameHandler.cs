using Monopoly.Core.Interface;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Notifications;
using Monopoly.Core.Randomness;

namespace Monopoly.Core;

internal sealed class GameHandler
{
    private readonly Game CurrentGame;

    internal GameHandler(Game currentGame)
    {
        CurrentGame = currentGame ?? throw new ArgumentNullException(nameof(currentGame));
    }

    public DiceRoll RoleDiceAndMovePlayer(Player player, RandomPurpose purpose = RandomPurpose.TurnDice)
    {
        DiceRoll roll = RollDice(player, purpose);
        MovePlayerAndInvokeEvent(player, player.Position + roll.Sum);
        return roll;
    }

    private void MovePlayerToBoardPosition(Player player, int targetPosition)
    {
        int boardSize = CurrentGame.Board.Track.Count;

        if (targetPosition >= boardSize)
        {
            int laps = targetPosition / boardSize;
            player.MoveTo(CurrentGame.Board.Track.NormalizeIndex(targetPosition));
            for (int i = 0; i < laps; i++)
                CurrentGame.Transactions.PlayerGetSalary(player);
        }
        else if (targetPosition < 0)
        {
            player.MoveTo(CurrentGame.Board.Track.NormalizeIndex(targetPosition));
        }
        else
            player.MoveTo(targetPosition);
    }

    public void MovePlayerAndInvokeEvent(Player player, int newPosition)
    {
        MovePlayerToBoardPosition(player, newPosition);
        CurrentGame.PublishNotification(new BoardChangedNotification());
    }

    public int GetPlayerGoPastGoNewPosition(int targetPosition)
    {
        return CurrentGame.Board.Track.Count + targetPosition;
    }

    internal DiceRoll PrepareDiceRoll(RandomPurpose purpose)
    {
        if (!DiceRoll.IsDicePurpose(purpose))
            throw new ArgumentException("The random purpose does not describe a dice roll.", nameof(purpose));

        int[] results = new int[CurrentGame.Rules.NumberOfDice];
        for (int index = 0; index < results.Length; index++)
        {
            results[index] = CurrentGame.Randomizer.NextInt(new RandomRequest(
                purpose,
                1,
                checked(CurrentGame.Rules.DieSides + 1),
                index));
        }

        return new DiceRoll(purpose, results, CurrentGame.Rules.DieSides);
    }

    internal void CommitDiceRoll(Player player, DiceRoll roll)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(roll);
        CurrentGame.CommitDiceRoll(roll);
        CurrentGame.LogWriter.CreateLog($"{player.Name} rolled: {string.Join(' ', roll.Results)} Total: {roll.Sum}");
    }

    public DiceRoll RollDice(Player player, RandomPurpose purpose = RandomPurpose.TurnDice)
    {
        DiceRoll roll = PrepareDiceRoll(purpose);
        CommitDiceRoll(player, roll);
        return roll;
    }

    public int GetMoneyFromBankruptPlayerAndBankruptPlayer(Player player)
    {
        int remainingPlayerMoney = CalculatePlayerAssets(player);
        player.TakeAllMoney();
        player.TakeAllJailCards();
        ClearOwnershipForPlayer(player);
        player.MarkBankrupt();
        RemoveBankruptPlayerFromGame(player);
        return remainingPlayerMoney;
    }

    public void HandlePlayerBankruptcy(Player player, string reason = "")
    {
        if (player.IsBankrupt)
        {
            string existingBankruptcyReason = FormatBankruptcyReason(player, reason);
            CurrentGame.LogWriter.CreateLog(existingBankruptcyReason);
            return;
        }

        DeclareBankruptcy(player, null, reason);
    }

    public void HandlePlayerBankruptcy(Player player, Player? creditor, string reason = "")
    {
        DeclareBankruptcy(player, creditor, reason);
    }

    public void DeclareBankruptcy(Player player, Player? creditor, string reason = "")
    {
        List<Square> ownedSquares = CurrentGame.Board.Squares.Where(square => square.Owner == player).ToList();
        int houseSaleValue = ownedSquares
            .OfType<PropertySquare>()
            .Sum(CalculateHouseAndHotelValue);
        int transferredMoney = checked(player.Money + houseSaleValue);
        if (creditor is not null &&
            (creditor.Money > int.MaxValue - transferredMoney ||
             creditor.NumberOfGetOutOFJailCards > int.MaxValue - player.NumberOfGetOutOFJailCards))
            throw new InvalidOperationException("The creditor cannot receive the bankrupt player's assets.");

        foreach (Square square in ownedSquares)
        {
            if (square is PropertySquare property)
                property.ClearBuildings();

            if (creditor is null)
                square.ReturnToBank();
            else
                square.TransferOwnership(creditor);
        }

        if (creditor is not null)
        {
            player.TakeAllMoney();
            creditor.Credit(transferredMoney);
            creditor.AddJailCards(player.TakeAllJailCards());
        }
        else
        {
            player.TakeAllMoney();
            player.TakeAllJailCards();
        }

        player.MarkBankrupt();
        RemoveBankruptPlayerFromGame(player);

        CurrentGame.LogWriter.CreateLog(FormatBankruptcyReason(player, reason));
    }

    public void ClearOwnershipForPlayer(Player player)
    {
        foreach (Square square in CurrentGame.Board.Squares)
        {
            if (square.Owner != player) continue;

            square.ReturnToBank();
            if (square is PropertySquare property)
                property.ClearBuildings();
        }
    }

    public bool IsPlayerBankrupt(Player player, int sum) => !CanAffordWithAssets(player, sum);

    public bool CanAffordWithAssets(Player player, int sum) => CalculatePlayerAssets(player) >= sum;

    public int CalculatePlayerAssets(Player player)
    {
        int totalAssets = player.Money;

        foreach (Square square in CurrentGame.Board.Squares)
        {
            if (square.Owner != player) continue;

            if (!square.IsMortgage)
                totalAssets += CalculateMortgageValue(square);

            if (square is PropertySquare property)
                totalAssets += CalculateHouseAndHotelValue(property);
        }

        return totalAssets;
    }

    public int CalculateMortgageValue(Square square) => square.MortgageValue;

    public int CalculateHouseAndHotelValue(PropertySquare property)
    {
        int value = 0;
        for (int i = 1; i <= property.Houses; i++)
            value += i == 5 ? property.BuildHotelCost / 2 : property.BuildHouseCost / 2;
        return value;
    }

    /// <summary>
    /// Tries to resolve a payment through the injected frontend provider.
    /// If the provider cannot make progress, the debtor is bankrupted.
    /// </summary>
    public bool TryResolvePayment(Player player, int amount, Player? creditor, string bankruptcyReason, bool payToFines = false)
    {
        if (!EnsureFunds(player, amount, creditor, bankruptcyReason)) return false;

        if (creditor is null && payToFines)
            CurrentGame.Transactions.PayFines(player, amount);
        else if (creditor is null)
            CurrentGame.Transactions.PayMoneyToBank(player, amount);
        else
            CurrentGame.Transactions.PayPlayerFromPlayer(player, amount, creditor);
        return true;
    }

    public bool EnsureFunds(Player player, int amount, Player? creditor, string bankruptcyReason)
    {
        if (amount <= 0) return true;

        while (player.Money < amount)
        {
            if (!CanAffordWithAssets(player, amount))
            {
                DeclareBankruptcy(player, creditor, bankruptcyReason);
                return false;
            }

            int moneyBefore = player.Money;
            bool madeProgress = CurrentGame.Decisions.ResolveInsufficientFunds(CurrentGame, player, amount);
            if (!madeProgress || player.Money <= moneyBefore)
            {
                DeclareBankruptcy(player, creditor, bankruptcyReason);
                return false;
            }
        }

        return true;
    }

    [Obsolete("Use TryResolvePayment instead.")]
    public bool IfPlayerCantPayInvokeOrBankrupt(Player player, int sum)
    {
        while (player.Money < sum)
        {
            if (!CanAffordWithAssets(player, sum))
            {
                HandlePlayerBankruptcy(player);
                return true;
            }

            int moneyBefore = player.Money;
            bool madeProgress = CurrentGame.Decisions.ResolveInsufficientFunds(CurrentGame, player, sum);
            if (!madeProgress || player.Money <= moneyBefore)
            {
                HandlePlayerBankruptcy(player);
                return true;
            }
        }

        return false;
    }

    private void RemoveBankruptPlayerFromGame(Player player)
    {
        if (CurrentGame.Players.Contains(player))
            CurrentGame.RemovePlayer(player);
    }

    private static string FormatBankruptcyReason(Player player, string reason)
    {
        if (string.IsNullOrEmpty(reason)) return $"{player.Name} has been bankrupt.";
        return $"{player.Name} has been bankrupt" + (reason.StartsWith(',') ? string.Empty : " ") + $"{reason}.";
    }
}
