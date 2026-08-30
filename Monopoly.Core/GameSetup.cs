using Monopoly.Core.Logs;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Randomness;

namespace Monopoly.Core;

public enum GameSetupErrorKind
{
    InvalidProfile,
    InvalidPlayerCount,
    InvalidPlayer,
    DuplicatePlayer,
    UnsupportedComponent,
    UnsupportedPolicy,
    StartingPlayerTieLimitExceeded
}

public sealed class GameSetupException : Exception
{
    public GameSetupException(GameSetupErrorKind kind, string path, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Kind = kind;
        Path = path;
    }

    public GameSetupErrorKind Kind { get; }
    public string Path { get; }
}

public sealed record PlayerSetup
{
    public PlayerSetup(int id, string name)
    {
        if (id < 0)
            throw new GameSetupException(GameSetupErrorKind.InvalidPlayer, "players.id", "A player ID must be non-negative.");
        if (string.IsNullOrWhiteSpace(name))
            throw new GameSetupException(GameSetupErrorKind.InvalidPlayer, "players.name", "A player name is required.");
        Id = id;
        Name = name;
    }

    public int Id { get; }
    public string Name { get; }
}

/// <summary>Creates a match only from an explicitly supplied validated profile.</summary>
public static class GameSetup
{
    private const int MaximumHighestRollRounds = 128;

    public static Game Create(
        ValidatedGameProfile profile,
        IEnumerable<PlayerSetup> players,
        IMatchRandomSource? randomSource = null) =>
        Create(profile, players, randomSource, ProfileComponentRegistry.CreateExecutionBaseline());

    internal static Game Create(
        ValidatedGameProfile profile,
        IEnumerable<PlayerSetup> players,
        IMatchRandomSource? randomSource,
        ProfileComponentRegistry registry)
    {
        if (profile is null)
            throw Error(GameSetupErrorKind.InvalidProfile, "profile", "A validated profile is required.");
        if (players is null)
            throw Error(GameSetupErrorKind.InvalidPlayer, "players", "Player setup input is required.");
        ArgumentNullException.ThrowIfNull(registry);

        PlayerSetup[] roster = players.ToArray();
        if (roster.Any(player => player is null))
            throw Error(GameSetupErrorKind.InvalidPlayer, "players", "Player setup entries cannot be null.");

        ValidateProfile(profile);
        ValidateRoster(profile, roster);
        registry.Validate(profile);

        MatchRandomizer randomizer = new(randomSource ?? new SystemMatchRandomSource());
        DeckRuntime decks = DeckRuntime.CreateForProfile(profile.RuleGraph.Decks, randomizer, shuffleDecks: true);

        int startIndex = profile.RuleGraph.Track.GetIndex(profile.Setup.StartSpaceId);
        Player[] runtimePlayers = roster.Select(playerSetup =>
        {
            Player player = new(playerSetup.Name, playerSetup.Id);
            player.InitializeProfileState(profile.Setup.StartingResources, profile.Setup.StartSpaceId, startIndex);
            return player;
        }).ToArray();
        Player currentPlayer = SelectStartingPlayer(profile.Setup, runtimePlayers, randomizer);

        GameBoard board = new(profile.RuleGraph.Spaces);
        SpaceId[] ownableSpaceIds = profile.RuleGraph.Spaces
            .Where(space => space.Capabilities.Contains(CapabilityKinds.Ownable))
            .Select(space => space.Id)
            .ToArray();

        return new Game(
            profile,
            runtimePlayers,
            currentPlayer,
            board,
            decks,
            randomizer,
            registry,
            ownableSpaceIds,
            new LogHandler());
    }

    private static void ValidateProfile(ValidatedGameProfile profile)
    {
        if (!profile.Id.IsValid)
            throw Error(GameSetupErrorKind.InvalidProfile, "profile.id", "The profile ID is invalid.");
        if (!profile.Revision.IsValid)
            throw Error(GameSetupErrorKind.InvalidProfile, "profile.revision", "The profile revision is invalid.");
        if (!profile.Fingerprint.IsValid)
            throw Error(GameSetupErrorKind.InvalidProfile, "profile.fingerprint", "The profile fingerprint is invalid.");
        if (profile.Setup.DieSides == int.MaxValue)
            throw Error(GameSetupErrorKind.InvalidProfile, "profile.setup.dieSides", "The die side count cannot be represented by the exclusive random-source range.");
        if (profile.RuleGraph.Track.Count != profile.RuleGraph.Spaces.Count ||
            profile.RuleGraph.Spaces.Where((space, index) => space.Id != profile.RuleGraph.Track.GetSpaceIdAt(index)).Any())
        {
            throw Error(GameSetupErrorKind.InvalidProfile, "profile.ruleGraph", "The profile spaces do not match its track.");
        }

        try
        {
            _ = profile.RuleGraph.Track.GetIndex(profile.Setup.StartSpaceId);
        }
        catch (KeyNotFoundException exception)
        {
            throw new GameSetupException(
                GameSetupErrorKind.InvalidProfile,
                "profile.setup.startSpaceId",
                exception.Message,
                exception);
        }
    }

    private static void ValidateRoster(ValidatedGameProfile profile, PlayerSetup[] roster)
    {
        if (roster.Length < profile.Setup.MinimumPlayers || roster.Length > profile.Setup.MaximumPlayers)
            throw Error(GameSetupErrorKind.InvalidPlayerCount, "players", $"The profile requires between {profile.Setup.MinimumPlayers} and {profile.Setup.MaximumPlayers} players.");

        HashSet<int> ids = [];
        for (int index = 0; index < roster.Length; index++)
        {
            PlayerSetup player = roster[index];
            if (player.Id < 0)
                throw Error(GameSetupErrorKind.InvalidPlayer, $"players[{index}].id", "A player ID must be non-negative.");
            if (string.IsNullOrWhiteSpace(player.Name))
                throw Error(GameSetupErrorKind.InvalidPlayer, $"players[{index}].name", "A player name is required.");
            if (!ids.Add(player.Id))
                throw Error(GameSetupErrorKind.DuplicatePlayer, $"players[{index}].id", $"Player ID '{player.Id}' is duplicated.");
        }
    }

    private static Player SelectStartingPlayer(
        ProfileSetupDefinition setup,
        IReadOnlyList<Player> players,
        MatchRandomizer randomizer) => setup.StartingPlayerPolicy switch
        {
            StartingPlayerPolicyKind.FixedOrder => players[0],
            StartingPlayerPolicyKind.Random => players[randomizer.NextInt(new RandomRequest(
                RandomPurpose.SetupStartingPlayer,
                0,
                players.Count,
                0))],
            StartingPlayerPolicyKind.HighestRoll => SelectHighestRollPlayer(setup, players, randomizer),
            _ => throw Error(GameSetupErrorKind.UnsupportedPolicy, "profile.setup.startingPlayerPolicy", $"Starting-player policy '{setup.StartingPlayerPolicy}' is not supported.")
        };

    private static Player SelectHighestRollPlayer(
        ProfileSetupDefinition setup,
        IReadOnlyList<Player> players,
        MatchRandomizer randomizer)
    {
        Player[] candidates = players.ToArray();
        int sequenceIndex = 0;

        for (int round = 0; round < MaximumHighestRollRounds; round++)
        {
            List<(Player Player, long Total)> totals = new(candidates.Length);
            foreach (Player candidate in candidates)
            {
                long total = 0;
                for (int die = 0; die < setup.DiceCount; die++)
                {
                    total += randomizer.NextInt(new RandomRequest(
                        RandomPurpose.SetupDice,
                        1,
                        checked(setup.DieSides + 1),
                        sequenceIndex++));
                }
                totals.Add((candidate, total));
            }

            long highest = totals.Max(result => result.Total);
            candidates = totals.Where(result => result.Total == highest).Select(result => result.Player).ToArray();
            if (candidates.Length == 1) return candidates[0];
        }

        throw Error(
            GameSetupErrorKind.StartingPlayerTieLimitExceeded,
            "profile.setup.startingPlayerPolicy",
            $"The highest-roll policy remained tied for {MaximumHighestRollRounds} rounds.");
    }

    private static GameSetupException Error(GameSetupErrorKind kind, string path, string message) =>
        new(kind, path, message);
}
