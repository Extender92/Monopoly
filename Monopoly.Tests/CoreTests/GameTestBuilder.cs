using Monopoly.Core.Interface;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Models.FortuneCard;
using Monopoly.Core.Presentation;
using Monopoly.Core.Persistence;
using System.Text.Json;

namespace Monopoly.Tests.CoreTests;

internal sealed class GameTestBuilder
{
    internal const int TrackLength = 17;
    internal const int DetentionPosition = 10;

    private readonly GameRules _rules;
    private readonly Dictionary<int, PlayerConfiguration> _players;
    private readonly Dictionary<int, SquareConfiguration> _squares = [];
    private readonly Dictionary<int, int> _detainedPlayers = [];
    private IPlayerDecisionProvider? _decisions;
    private IMatchRandomSource _randomSource = new MinimumMatchRandomSource();
    private int _currentPlayerId;
    private int _currentTurn = 1;
    private int _consecutiveDoubles;
    private int _fines;
    private ProfilePresentation? _presentation;

    internal GameTestBuilder(int playerCount = 2)
        : this(new GameRules(playerCount, 2, 6))
    {
    }

    internal GameTestBuilder(GameRules rules)
    {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _players = Enumerable.Range(0, rules.NumberOfPlayers)
            .ToDictionary(
                id => id,
                id => new PlayerConfiguration($"Player {id + 1}", 3000, 0, 0, false));
    }

    internal GameTestBuilder WithCurrentPlayer(int playerId)
    {
        _currentPlayerId = playerId;
        return this;
    }

    internal GameTestBuilder WithPlayer(
        int playerId,
        string? name = null,
        int? money = null,
        int? position = null,
        int? jailCards = null,
        bool? isBankrupt = null)
    {
        PlayerConfiguration existing = _players[playerId];
        bool bankrupt = isBankrupt ?? existing.IsBankrupt;
        _players[playerId] = existing with
        {
            Name = name ?? existing.Name,
            Money = money ?? (bankrupt ? 0 : existing.Money),
            Position = position ?? existing.Position,
            HeldCards = jailCards ?? (bankrupt ? 0 : existing.HeldCards),
            IsBankrupt = bankrupt
        };
        return this;
    }

    internal GameTestBuilder WithTurn(int currentTurn, int consecutiveDoubles = 0, int fines = 0)
    {
        _currentTurn = currentTurn;
        _consecutiveDoubles = consecutiveDoubles;
        _fines = fines;
        return this;
    }

    internal GameTestBuilder WithSquare(
        int position,
        int? ownerId,
        int houses = 0,
        bool isMortgage = false)
    {
        _squares[position] = new SquareConfiguration(ownerId, houses, isMortgage);
        return this;
    }

    internal GameTestBuilder WithPlayerInJail(int playerId, int turnsInJail = 0)
    {
        WithPlayer(playerId, position: DetentionPosition);
        _detainedPlayers[playerId] = turnsInJail;
        return this;
    }

    internal GameTestBuilder WithRandomValues(params int[] values)
    {
        _randomSource = new ScriptedMatchRandomSource(values);
        return this;
    }

    internal GameTestBuilder WithRandomSource(IMatchRandomSource randomSource)
    {
        _randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
        return this;
    }

    internal GameTestBuilder WithDecisions(IPlayerDecisionProvider decisions)
    {
        _decisions = decisions;
        return this;
    }

    internal GameTestBuilder WithPresentation(ProfilePresentation presentation)
    {
        _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
        return this;
    }

    internal Game Build()
    {
        List<Player> players = _players
            .OrderBy(entry => entry.Key)
            .Select(entry => CreatePlayer(entry.Key, entry.Value))
            .ToList();
        Dictionary<int, Player> playersById = players.ToDictionary(player => player.Id);
        Player currentPlayer = playersById[_currentPlayerId];
        SyntheticRuntime runtime = SyntheticRuntime.Create();
        Game game = new(
            players,
            currentPlayer,
            _rules,
            runtime.Board,
            runtime.Decks,
            DetentionPosition,
            _presentation ?? runtime.Presentation,
            _decisions,
            _randomSource,
            shuffleDecks: false);

        game.RestoreTurnState(_fines, _currentTurn, _consecutiveDoubles);
        foreach ((int position, SquareConfiguration state) in _squares)
        {
            Square square = game.Board.GetSquareAtPosition(position);
            Player? owner = state.OwnerId is int ownerId ? playersById[ownerId] : null;
            square.RestoreState(owner, state.IsMortgage, state.Houses);
        }

        foreach ((int playerId, int turns) in _detainedPlayers)
            game.TheJail.RestorePlayerInJail(playersById[playerId], turns);

        List<Player> activePlayers = players.Where(player => !player.IsBankrupt).ToList();
        if (activePlayers.Count <= 1)
            game.RestoreWinner(activePlayers.SingleOrDefault());

        game.ValidateAuthoritativeState();
        game.ResetReconstructedProgress();
        return game;
    }

    private static Player CreatePlayer(int id, PlayerConfiguration configuration)
    {
        Player player = new(configuration.Name, id);
        player.RestoreState(
            configuration.Money,
            configuration.Position,
            configuration.HeldCards,
            configuration.IsBankrupt);
        return player;
    }

    private sealed record PlayerConfiguration(
        string Name,
        int Money,
        int Position,
        int HeldCards,
        bool IsBankrupt);

    private sealed record SquareConfiguration(int? OwnerId, int Houses, bool IsMortgage);

    private sealed record SyntheticRuntime(
        GameBoard Board,
        IReadOnlyList<RuntimeDeckRegistration> Decks,
        ProfilePresentation Presentation)
    {
        internal static SyntheticRuntime Create()
        {
            PresentationMetadata groupA = Metadata("group.a", "Moss Makers");
            PresentationMetadata groupB = Metadata("group.b", "River Makers");
            PresentationMetadata groupC = Metadata("group.c", "Light Makers");
            DeckId deckId = new("deck.test-events");
            PresentationToken deckToken = new("deck.test-events");

            List<Square> squares =
            [
                new GoSquare(0, Metadata("space.origin", "Route Origin")),
                Property(1, "Moss Studio", "group.a", groupA, 10, 20),
                new DrawSquare(2, Metadata("space.message-a", "Message Post"), deckId),
                Property(3, "Reed Studio", "group.a", groupA, 12, 22),
                new TaxSquare(4, 20, Metadata("space.route-fee", "Route Fee")),
                new RailroadSquare(5, Metadata("space.carrier", "Shared Carrier"), 30, 4, 8, 12, 16, 15),
                Property(6, "River Studio", "group.b", groupB, 14, 24),
                new DrawSquare(7, Metadata("space.message-b", "Workshop Notice"), deckId),
                Property(8, "Wheel Studio", "group.b", groupB, 15, 26),
                Property(9, "Cloud Studio", "group.b", groupB, 16, 28),
                new JailSquare(10, Metadata("space.pause", "Quiet Pause")),
                Property(11, "Lantern Studio", "group.c", groupC, 18, 30),
                new UtilitySquare(12, Metadata("space.service", "Route Service"), 25, 2, 4, 12),
                Property(13, "Beacon Studio", "group.c", groupC, 20, 32),
                Property(14, "Vale Studio", "group.c", groupC, 22, 34),
                new GoToJailSquare(15, Metadata("space.redirect", "Route Redirect")),
                new ParkingSquare(16, Metadata("space.rest", "Rest Stop"))
            ];

            RuntimeDeckRegistration deck = new(
                deckId,
                deckToken,
                [
                    Card("card.test-a", "card.test-a"),
                    Card("card.test-b", "card.test-b"),
                    Card("card.test-c", "card.test-c")
                ]);

            List<PresentationMetadata> catalog =
            [
                new(PresentationTokens.PrimaryResource, "Credits", symbol: "C"),
                new(PresentationTokens.PropertyPurchaseDecision, "Purchase workshop"),
                new(PresentationTokens.DetentionReleaseDecision, "Resolve pause"),
                new(PresentationTokens.DetainedStatus, "Paused"),
                new(PresentationTokens.LogNotification),
                new(PresentationTokens.BoardNotification),
                new(PresentationTokens.PlayerInformationNotification),
                groupA,
                groupB,
                groupC,
                new(deckToken, "Workshop messages"),
                Metadata("card.test-a", "Message A"),
                Metadata("card.test-b", "Message B"),
                Metadata("card.test-c", "Message C"),
                .. squares.Select(square => square.Presentation)
            ];

            return new SyntheticRuntime(new GameBoard(squares), [deck], new ProfilePresentation(catalog));
        }

        private static PropertySquare Property(
            int position,
            string name,
            string groupId,
            PresentationMetadata groupPresentation,
            int fee,
            int price) =>
            new(
                new GroupId(groupId),
                groupPresentation,
                Metadata($"space.property-{position}", name),
                fee,
                fee * 2,
                fee * 3,
                fee * 4,
                fee * 5,
                fee * 6,
                fee * 8,
                10,
                10,
                price,
                price / 2,
                position);

        private static RuntimeCardRegistration Card(string id, string token) =>
            new(new CardId(id), new NoOpLegacyCard(new PresentationToken(token)));

        private static PresentationMetadata Metadata(string token, string displayText) =>
            new(new PresentationToken(token), displayText);
    }

    private sealed class NoOpLegacyCard(PresentationToken presentationToken) : ILegacyCard
    {
        public PresentationToken PresentationToken { get; } = presentationToken;
        public void ExecuteEffect(Player player, Game game)
        {
        }
    }
}

internal static class GameTestSnapshot
{
    internal static string CaptureAuthoritative(Game game) => JsonSerializer.Serialize(new
    {
        Players = game.Players.Select(player => new
        {
            player.Id,
            player.Money,
            player.Position,
            player.NumberOfGetOutOFJailCards,
            player.IsBankrupt
        }),
        Squares = game.Board.Squares.Select(square => new
        {
            square.Id,
            OwnerId = square.Owner?.Id,
            square.IsMortgage,
            Houses = square is PropertySquare property ? property.Houses : 0
        }),
        Decks = game.Decks.Entries.Select(deck => new
        {
            deck.Id,
            Cards = deck.Cards.Select(card => card.Id)
        }),
        Statuses = game.Statuses.Entries,
        CurrentPlayerId = game.CurrentPlayer.Id,
        game.CurrentTurn,
        game.ConsecutiveDoubles,
        game.Fines,
        WinnerId = game.Winner?.Id,
        Dice = game.LastDiceRoll?.Results
    });

    internal static string Capture(Game game) => JsonSerializer.Serialize(new
    {
        Players = game.Players.Select(player => new
        {
            player.Id,
            player.Name,
            player.Money,
            player.Position,
            player.NumberOfGetOutOFJailCards,
            player.IsBankrupt
        }),
        Squares = game.Board.Squares.Select(square => new
        {
            square.Id,
            OwnerId = square.Owner?.Id,
            square.IsMortgage,
            Houses = square is PropertySquare property ? property.Houses : 0
        }),
        Decks = game.Decks.Entries.Select(deck => new
        {
            deck.Id,
            Cards = deck.Cards.Select(card => card.Id)
        }),
        Statuses = game.Statuses.Entries,
        CurrentPlayerId = game.CurrentPlayer.Id,
        game.CurrentTurn,
        game.ConsecutiveDoubles,
        game.Fines,
        WinnerId = game.Winner?.Id,
        Dice = game.LastDiceRoll?.Results,
        Logs = game.Logs.LogList.Select(log => new { log.Id, log.Info }),
        Progress = GameProgressStateMapper.ToState(game)
    });
}

internal static class GameTestActions
{
    internal static TurnResult PlayTurnToCompletion(
        this Game game,
        Func<PendingDecision, DecisionOptionId>? chooseResponse = null)
    {
        GameActionResult result = game.PlayTurn();
        while (result.Status == GameActionStatus.DecisionRequired)
        {
            PendingDecision decision = result.PendingDecision
                ?? throw new InvalidOperationException("A required decision did not contain a snapshot.");
            DecisionOptionId response = chooseResponse?.Invoke(decision) ?? decision switch
            {
                PurchaseDecision => DecisionOptions.Decline,
                StatusDecision => DecisionOptions.Roll,
                _ => throw new InvalidOperationException("Unknown test decision type.")
            };
            result = game.SubmitDecision(new DecisionResponse(decision.DecisionId, response));
        }

        return result.TurnResult
            ?? throw new InvalidOperationException($"The test turn did not complete: {result.Status} ({result.RejectionReason}).");
    }
}
