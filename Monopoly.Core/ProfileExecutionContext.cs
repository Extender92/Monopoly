using Monopoly.Core.Notifications;
using Monopoly.Core.Presentation;
using Monopoly.Core.Randomness;

namespace Monopoly.Core;

internal sealed class ProfileExecutionContext
{
    private readonly Game _game;
    private readonly ExecutionTransition _transition;
    private SpaceDefinition _currentSpace;

    internal ProfileExecutionContext(Game game, ExecutionTransition transition, int actorPlayerId, DiceRoll roll)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        _transition = transition ?? throw new ArgumentNullException(nameof(transition));
        ActorPlayerId = actorPlayerId;
        Roll = roll ?? throw new ArgumentNullException(nameof(roll));
        _currentSpace = _game.Board.GetDefinition(CurrentSpaceId);
    }

    internal int ActorPlayerId { get; }
    internal DiceRoll Roll { get; }
    internal PreparedPlayerState Actor => _transition.Players[ActorPlayerId];
    internal SpaceId CurrentSpaceId => Actor.SpaceId;

    internal void ResolveLanding(SpaceId spaceId, int startCapabilityIndex)
    {
        _currentSpace = _game.Board.GetDefinition(spaceId);
        IReadOnlyList<CapabilityDefinition> capabilities = _game.Registry.OrderLandingCapabilities(_currentSpace.Capabilities);
        if (startCapabilityIndex < 0 || startCapabilityIndex > capabilities.Count)
            throw ExecutionError(ProfileExecutionErrorKind.InvalidRuntimeState, "turn.continuation.capabilityIndex", "The landing continuation index is invalid.");

        for (int index = startCapabilityIndex; index < capabilities.Count; index++)
        {
            _game.Registry.ExecuteCapability(this, capabilities[index], index);
            if (_transition.PendingDecision is not null)
                return;
        }
    }

    internal void MoveByOffset(long offset, bool applyOriginReward, string path)
    {
        int originIndex = Actor.Position;
        long rawTarget = (long)originIndex + offset;
        int targetIndex = _game.Board.Track.NormalizeIndex(rawTarget);
        int passes = offset > 0 ? checked((int)(rawTarget / _game.Board.Track.Count)) : 0;
        MoveTo(targetIndex, passes, applyOriginReward, path);
    }

    internal void ApplyPurchasable(PurchasableCapabilityDefinition capability, int capabilityIndex)
    {
        if (!_transition.Ownership.TryGetValue(_currentSpace.Id, out int? ownerId) || ownerId is not null)
            return;
        if (Actor.Resources[capability.Price.ResourceId] < capability.Price.Value)
        {
            _game.Registry.ExecutePurchaseNonPurchase(
                this,
                _game.Profile.Policies.PurchaseDecline,
                PurchaseNonPurchaseReason.InsufficientResources);
            return;
        }

        PurchaseDecision decision = new(
            Guid.NewGuid(),
            ActorPlayerId,
            _currentSpace.Id,
            capability.Price,
            _currentSpace.PresentationToken);
        _transition.PendingDecision = decision;
        _transition.Continuation = new TurnContinuation(
            ActorPlayerId,
            Roll,
            _currentSpace.Id,
            capabilityIndex + 1);
    }

    internal void ApplyPurchase(PurchaseDecision decision)
    {
        if (decision.PlayerId != ActorPlayerId || decision.SpaceId != CurrentSpaceId ||
            !_transition.Ownership.TryGetValue(decision.SpaceId, out int? previous) || previous is not null)
        {
            throw ExecutionError(ProfileExecutionErrorKind.InvalidRuntimeState, "decision.purchase", "The purchase preconditions changed before commit.");
        }

        DebitExact(ActorPlayerId, decision.Price.ResourceId, decision.Price.Value, "decision.purchase.price");
        _transition.Ownership[decision.SpaceId] = ActorPlayerId;
        _transition.Notifications.Add(new OwnershipChangedNotification(
            decision.SpaceId,
            previous,
            ActorPlayerId,
            _game.Board.GetSpace(decision.SpaceId).PresentationToken));
    }

    internal void ApplyUsageFee(UsageFeeCapabilityDefinition capability)
    {
        if (!_transition.Ownership.TryGetValue(_currentSpace.Id, out int? ownerId) ||
            ownerId is null || ownerId == ActorPlayerId)
        {
            return;
        }

        int available = Actor.Resources[capability.Amount.ResourceId];
        int transferred = Math.Min(available, capability.Amount.Value);
        if (transferred == 0) return;

        PreparedPlayerState owner = _transition.Players[ownerId.Value];
        EnsureCreditFits(owner, capability.Amount.ResourceId, transferred, "capability.usage-fee");
        DebitBounded(ActorPlayerId, capability.Amount.ResourceId, transferred);
        Credit(owner.PlayerId, capability.Amount.ResourceId, transferred, "capability.usage-fee");
    }

    internal void ApplyDraw(DrawCapabilityDefinition capability)
    {
        List<CardDefinition> cards = _transition.DeckOrders[capability.DeckId];
        CardDefinition card = cards[0];
        cards.RemoveAt(0);
        cards.Add(card);

        DeckDefinition deck = _game.Profile.RuleGraph.Decks.Single(candidate => candidate.Id == capability.DeckId);
        _transition.Notifications.Add(new CardDrawnNotification(
            new CardView(card.Id, card.PresentationToken),
            deck.Id,
            deck.PresentationToken));

        for (int index = 0; index < card.Effects.Entries.Count; index++)
        {
            EffectDefinition effect = card.Effects.Entries[index];
            _game.Registry.ExecuteEffect(this, effect, $"card.{card.Id}.effects[{index}]");
            if (_transition.PendingDecision is not null) return;
        }
    }

    internal void ApplyMoveEffect(MoveEffectDefinition effect, string path)
    {
        int originIndex = Actor.Position;
        int targetIndex;
        int passes = 0;
        switch (effect.Target)
        {
            case RelativeMoveTarget relative:
                long rawTarget = (long)originIndex + relative.Offset;
                targetIndex = _game.Board.Track.NormalizeIndex(rawTarget);
                if (relative.Offset > 0)
                    passes = checked((int)(rawTarget / _game.Board.Track.Count));
                break;
            case AbsoluteMoveTarget absolute:
                targetIndex = _game.Board.Track.GetIndex(absolute.SpaceId);
                passes = targetIndex < originIndex ? 1 : 0;
                break;
            default:
                throw ExecutionError(ProfileExecutionErrorKind.UnsupportedExecutionShape, path, "The movement target is not supported.");
        }

        MoveTo(
            targetIndex,
            passes,
            effect.PassOriginPolicy == PassOriginPolicy.ApplyProfileReward,
            path);
        if (effect.ResolveDestination)
            ResolveLanding(CurrentSpaceId, 0);
    }

    internal void ApplyResourceChange(ResourceChangeEffectDefinition effect, string path)
    {
        if (effect.Delta > 0)
            Credit(ActorPlayerId, effect.ResourceId, effect.Delta, path);
        else
            DebitBounded(ActorPlayerId, effect.ResourceId, Math.Min((long)int.MaxValue, -(long)effect.Delta));
    }

    internal void CompleteTurn()
    {
        int currentIndex = _game.Players.ToList().FindIndex(player => player.Id == ActorPlayerId);
        int nextIndex = (currentIndex + 1) % _game.Players.Count;
        int nextPlayerId = _game.Players[nextIndex].Id;
        bool completedRound = nextPlayerId == _game.RoundAnchorPlayerId;

        if (completedRound && _transition.RoundNumber >= _game.Profile.Policies.MatchEnd.RoundLimit)
        {
            ResourceId score = _game.Profile.Policies.MatchEnd.ScoreResourceId;
            int winnerId = _game.Registry.SelectRoundLimitedWinner(this, _game.Profile.Policies.MatchEnd);
            _transition.WinnerPlayerId = winnerId;
            _transition.Phase = GamePhase.GameOver;
            _transition.PendingDecision = null;
            _transition.Continuation = null;
            _transition.Notifications.Add(new MatchEndedNotification(
                winnerId,
                _transition.RoundNumber,
                score,
                _game.Profile.PresentationToken));
            return;
        }

        if (completedRound)
            _transition.RoundNumber++;
        _transition.CurrentPlayerId = nextPlayerId;
        _transition.Phase = GamePhase.ReadyForTurn;
        _transition.PendingDecision = null;
        _transition.Continuation = null;
        _transition.Notifications.Add(new TurnAdvancedNotification(
            nextPlayerId,
            _transition.RoundNumber,
            _game.Profile.PresentationToken));
    }

    private void MoveTo(int targetIndex, int originPasses, bool applyOriginReward, string path)
    {
        SpaceId from = Actor.SpaceId;
        SpaceId target = _game.Board.Track.GetSpaceIdAt(targetIndex);
        Actor.Position = targetIndex;
        Actor.SpaceId = target;

        if (applyOriginReward && originPasses > 0)
            _game.Registry.ExecutePassOriginReward(this, originPasses, path);

        _transition.Notifications.Add(new PlayerMovedNotification(
            ActorPlayerId,
            from,
            target,
            originPasses,
            _game.Board.GetSpace(target).PresentationToken));
        _currentSpace = _game.Board.GetDefinition(target);
    }

    internal void ApplyConfiguredOriginReward(int originPasses, string path)
    {
        if (originPasses <= 0 || _game.Profile.Policies.PassOriginReward is not ResourceAmount reward)
            return;
        long total = checked((long)reward.Value * originPasses);
        if (total > int.MaxValue)
            throw ExecutionError(ProfileExecutionErrorKind.ResourceOverflow, path, "The pass-origin reward exceeds the supported resource range.");
        Credit(ActorPlayerId, reward.ResourceId, (int)total, path);
    }

    internal void LeaveCurrentSpaceUnowned()
    {
        if (!_transition.Ownership.TryGetValue(_currentSpace.Id, out int? ownerId) || ownerId is not null)
            throw ExecutionError(ProfileExecutionErrorKind.InvalidRuntimeState, "policy.purchase-decline", "The leave-unowned policy requires an unowned current space.");
    }

    internal int SelectHighestResourceWinner(ResourceId scoreResourceId) =>
        _transition.Players.Values
            .OrderByDescending(player => player.Resources[scoreResourceId])
            .ThenBy(player => player.PlayerId)
            .First()
            .PlayerId;

    private void DebitBounded(int playerId, ResourceId resourceId, long requested)
    {
        PreparedPlayerState player = _transition.Players[playerId];
        int previous = player.Resources[resourceId];
        int debit = (int)Math.Min(previous, requested);
        if (debit == 0) return;
        player.Resources[resourceId] = previous - debit;
        _transition.Notifications.Add(new ResourceChangedNotification(
            playerId,
            resourceId,
            previous,
            player.Resources[resourceId],
            _game.ResourcePresentationToken(resourceId)));
    }

    private void DebitExact(int playerId, ResourceId resourceId, int requested, string path)
    {
        PreparedPlayerState player = _transition.Players[playerId];
        int previous = player.Resources[resourceId];
        if (requested < 0 || previous < requested)
            throw ExecutionError(ProfileExecutionErrorKind.InvalidRuntimeState, path, "The exact resource debit cannot be committed.");
        if (requested == 0) return;

        player.Resources[resourceId] = previous - requested;
        _transition.Notifications.Add(new ResourceChangedNotification(
            playerId,
            resourceId,
            previous,
            player.Resources[resourceId],
            _game.ResourcePresentationToken(resourceId)));
    }

    private void Credit(int playerId, ResourceId resourceId, int amount, string path)
    {
        PreparedPlayerState player = _transition.Players[playerId];
        EnsureCreditFits(player, resourceId, amount, path);
        int previous = player.Resources[resourceId];
        player.Resources[resourceId] = previous + amount;
        if (amount == 0) return;
        _transition.Notifications.Add(new ResourceChangedNotification(
            playerId,
            resourceId,
            previous,
            player.Resources[resourceId],
            _game.ResourcePresentationToken(resourceId)));
    }

    private static void EnsureCreditFits(PreparedPlayerState player, ResourceId resourceId, int amount, string path)
    {
        if (amount < 0 || player.Resources[resourceId] > int.MaxValue - amount)
            throw ExecutionError(ProfileExecutionErrorKind.ResourceOverflow, path, $"Resource '{resourceId}' exceeds the supported range.");
    }

    private static ProfileExecutionException ExecutionError(ProfileExecutionErrorKind kind, string path, string message) =>
        new(kind, path, message);
}
