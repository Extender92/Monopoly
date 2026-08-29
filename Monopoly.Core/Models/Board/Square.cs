using Monopoly.Core.Presentation;

namespace Monopoly.Core.Models.Board;

public abstract class Square
{
    protected Square(int position, PresentationMetadata presentation)
    {
        if (position < 0) throw new ArgumentOutOfRangeException(nameof(position));
        Presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
        Position = position;
    }

    public int Position { get; }
    public PresentationToken PresentationToken => Presentation.Token;
    internal PresentationMetadata Presentation { get; }
    public Player? Owner { get; private set; }
    public int Price { get; protected set; }
    public int MortgageValue { get; protected set; }
    public bool IsMortgage { get; private set; }

    internal abstract void LandOn(Player player, Game game);

    internal void AssignOwner(Player owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (Owner is not null)
            throw new InvalidOperationException("An owned square cannot be purchased again.");
        Owner = owner;
    }

    internal void TransferOwnership(Player owner) =>
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));

    internal void ReturnToBank()
    {
        Owner = null;
        IsMortgage = false;
    }

    internal void PlaceMortgage()
    {
        if (Owner is null || IsMortgage)
            throw new InvalidOperationException("Only an owned, unmortgaged square can be mortgaged.");
        IsMortgage = true;
    }

    internal void RepayMortgage()
    {
        if (!IsMortgage)
            throw new InvalidOperationException("Only a mortgaged square can be repaid.");
        IsMortgage = false;
    }

    internal virtual void RestoreState(Player? owner, bool isMortgage, int houses)
    {
        if (houses != 0)
            throw new ArgumentOutOfRangeException(nameof(houses), "Only properties can contain buildings.");
        if (isMortgage && owner is null)
            throw new ArgumentException("A mortgaged square must have an owner.", nameof(isMortgage));
        Owner = owner;
        IsMortgage = isMortgage;
    }
}
