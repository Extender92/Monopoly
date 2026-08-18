using Monopoly.Core.Events;
using Monopoly.Core.Interface;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;

namespace Monopoly.Core;

public class GameHandler
{
    private readonly IGame CurrentGame;

    public GameHandler(IGame currentGame)
    {
        CurrentGame = currentGame;
    }

    public void RoleDiceAndMovePlayer(Player player)
    {
        RollDice(player);
        int diceSum = CalculateDiceSum();
        MovePlayerAndInvokeEvent(player, player.Position + diceSum);
    }

    public void CheckIfPlayerGoPastGo(Player player)
    {
        int boardSize = CurrentGame.Board.Squares.Count;
        if (boardSize == 0) throw new InvalidOperationException("The game board cannot be empty.");

        if (player.Position >= boardSize)
        {
            int laps = player.Position / boardSize;
            player.Position %= boardSize;
            for (int i = 0; i < laps; i++)
                CurrentGame.Transactions.PlayerGetSalary(player);
        }
        else if (player.Position < 0)
        {
            player.Position = ((player.Position % boardSize) + boardSize) % boardSize;
        }
    }

    public void MovePlayerAndInvokeEvent(Player player, int newPosition)
    {
        player.Position = newPosition;
        CheckIfPlayerGoPastGo(player);
        GameEvents.InvokeUpdateGameBoard(CurrentGame);
    }

    public int GetPlayerGoPastGoNewPosition(int targetPosition)
    {
        return CurrentGame.Board.Squares.Count + targetPosition;
    }

    public void RollDice(Player player)
    {
        string diceRoll = $"{player.Name} rolled:";
        foreach (IDie die in CurrentGame.Dice)
        {
            die.Roll();
            diceRoll += $" {die.GetDieResult()}";
        }

        diceRoll += $" Total: {CalculateDiceSum()}";
        CurrentGame.Logs.CreateLog(diceRoll);
    }

    public bool IsDiceDouble()
    {
        if (CurrentGame.Dice.Count < 2) return false;
        int firstDieResult = CurrentGame.Dice[0].GetDieResult();
        return CurrentGame.Dice.All(die => die.GetDieResult() == firstDieResult);
    }

    public int CalculateDiceSum() => CurrentGame.Dice.Sum(die => die.GetDieResult());

    public int GetMoneyFromBankruptPlayerAndBankruptPlayer(Player player)
    {
        int remainingPlayerMoney = CalculatePlayerAssets(player);
        player.Money = 0;
        ClearOwnershipForPlayer(player);
        player.IsBankrupt = true;
        RemoveBankruptPlayerFromGame(player);
        return remainingPlayerMoney;
    }

    public void HandlePlayerBankruptcy(Player player, string reason = "")
    {
        if (player.IsBankrupt)
        {
            string existingBankruptcyReason = FormatBankruptcyReason(player, reason);
            CurrentGame.Logs.CreateLog(existingBankruptcyReason);
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
        int houseSaleValue = 0;
        foreach (Square square in CurrentGame.Board.Squares.Where(square => square.Owner == player))
        {
            if (square is PropertySquare property)
            {
                houseSaleValue += CalculateHouseAndHotelValue(property);
                property.Houses = 0;
            }

            if (creditor is null)
            {
                square.Owner = null;
                square.IsMortgage = false;
            }
            else
            {
                square.Owner = creditor;
            }
        }

        if (creditor is not null)
        {
            creditor.Money += player.Money + houseSaleValue;
            creditor.NumberOfGetOutOFJailCards += player.NumberOfGetOutOFJailCards;
        }

        player.Money = 0;
        player.NumberOfGetOutOFJailCards = 0;
        player.IsBankrupt = true;
        RemoveBankruptPlayerFromGame(player);

        CurrentGame.Logs.CreateLog(FormatBankruptcyReason(player, reason));
    }

    public void ClearOwnershipForPlayer(Player player)
    {
        foreach (Square square in CurrentGame.Board.Squares)
        {
            if (square.Owner != player) continue;

            square.Owner = null;
            square.IsMortgage = false;
            if (square is PropertySquare property)
                property.Houses = 0;
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
            bool madeProgress = CurrentGame.Decisions.ResolveInsufficientFunds((Game)CurrentGame, player, amount);
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
            GameEvents.InvokePlayerInsufficientFunds(CurrentGame, player, sum);
            if (player.Money <= moneyBefore)
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
