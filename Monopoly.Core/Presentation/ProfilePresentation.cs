using System.Collections.ObjectModel;

namespace Monopoly.Core.Presentation;

/// <summary>An immutable, deterministically ordered presentation catalog.</summary>
public sealed class ProfilePresentation
{
    private readonly ReadOnlyDictionary<PresentationToken, PresentationMetadata> _byToken;
    private readonly ReadOnlyCollection<PresentationMetadata> _entries;

    public ProfilePresentation(IEnumerable<PresentationMetadata> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        Dictionary<PresentationToken, PresentationMetadata> byToken = [];
        foreach (PresentationMetadata? entry in entries)
        {
            if (entry is null)
                throw new ArgumentException("A presentation catalog cannot contain null entries.", nameof(entries));
            if (!entry.Token.IsValid)
                throw new ArgumentException("A presentation catalog contains an invalid token.", nameof(entries));
            if (!byToken.TryAdd(entry.Token, entry))
                throw new ArgumentException($"Presentation token '{entry.Token}' is duplicated or conflicting.", nameof(entries));
        }

        foreach (PresentationMetadata entry in byToken.Values)
        {
            EnsureHintExists(entry.Token, entry.ColorToken, "color", byToken);
            EnsureHintExists(entry.Token, entry.LayoutToken, "layout", byToken);
        }

        PresentationMetadata[] sorted = byToken.Values
            .OrderBy(entry => entry.Token)
            .ToArray();
        _entries = Array.AsReadOnly(sorted);
        _byToken = new ReadOnlyDictionary<PresentationToken, PresentationMetadata>(byToken);
    }

    public IReadOnlyList<PresentationMetadata> Entries => _entries;

    public PresentationMetadata Resolve(PresentationToken token)
    {
        if (!token.IsValid) throw new ArgumentException("The presentation token is invalid.", nameof(token));
        return _byToken.TryGetValue(token, out PresentationMetadata? metadata)
            ? metadata
            : throw new KeyNotFoundException($"Presentation token '{token}' is not defined by the profile.");
    }

    public bool TryResolve(PresentationToken token, out PresentationMetadata? metadata)
    {
        if (!token.IsValid)
        {
            metadata = null;
            return false;
        }

        return _byToken.TryGetValue(token, out metadata);
    }

    public string ResolveDisplayText(PresentationToken token)
    {
        PresentationMetadata metadata = Resolve(token);
        return metadata.DisplayText ?? metadata.ShortText ?? token.Value;
    }

    internal void EnsureReferences(IEnumerable<PresentationToken> references)
    {
        ArgumentNullException.ThrowIfNull(references);
        foreach (PresentationToken token in references)
        {
            if (!token.IsValid || !_byToken.ContainsKey(token))
                throw new ArgumentException($"Presentation token '{token}' is referenced but missing from the profile catalog.", nameof(references));
        }
    }

    private static void EnsureHintExists(
        PresentationToken owner,
        PresentationToken? hint,
        string kind,
        IReadOnlyDictionary<PresentationToken, PresentationMetadata> entries)
    {
        if (hint is not null && !entries.ContainsKey(hint.Value))
            throw new ArgumentException($"Presentation token '{owner}' references missing {kind} token '{hint}'.", nameof(entries));
    }
}
