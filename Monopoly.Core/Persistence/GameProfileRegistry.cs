using System.Collections.ObjectModel;

namespace Monopoly.Core.Persistence;

/// <summary>An immutable set of validated profiles that a save is allowed to reference.</summary>
public sealed class GameProfileRegistry
{
    private readonly ReadOnlyCollection<ValidatedGameProfile> _profiles;
    private readonly Dictionary<(ProfileId Id, ProfileRevision Revision), ValidatedGameProfile> _byIdentity;

    public GameProfileRegistry(IEnumerable<ValidatedGameProfile> profiles)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        ValidatedGameProfile[] entries = profiles.ToArray();
        if (entries.Length == 0)
            throw new ArgumentException("At least one validated profile must be registered.", nameof(profiles));
        if (entries.Any(profile => profile is null))
            throw new ArgumentException("Registered profiles cannot contain null entries.", nameof(profiles));

        _byIdentity = [];
        foreach (ValidatedGameProfile profile in entries)
        {
            var key = (profile.Id, profile.Revision);
            if (!_byIdentity.TryAdd(key, profile))
                throw new ArgumentException($"Profile '{profile.Id}' revision '{profile.Revision}' is registered more than once.", nameof(profiles));
        }

        _profiles = Array.AsReadOnly(entries
            .OrderBy(profile => profile.Id)
            .ThenBy(profile => profile.Revision)
            .ToArray());
    }

    public IReadOnlyList<ValidatedGameProfile> Profiles => _profiles;

    public ValidatedGameProfile ResolveExact(
        ProfileId profileId,
        ProfileRevision revision,
        ProfileFingerprint fingerprint)
    {
        if (!_byIdentity.TryGetValue((profileId, revision), out ValidatedGameProfile? profile))
        {
            throw new GameProfileResolutionException(
                GameProfileResolutionErrorKind.NotRegistered,
                $"Profile '{profileId}' revision '{revision}' is not registered.");
        }

        if (profile.Fingerprint != fingerprint)
        {
            throw new GameProfileResolutionException(
                GameProfileResolutionErrorKind.FingerprintMismatch,
                $"Profile '{profileId}' revision '{revision}' does not match the saved fingerprint.");
        }

        return profile;
    }
}
