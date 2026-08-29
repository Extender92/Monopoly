using System.Collections.ObjectModel;
using Monopoly.Core.Randomness;

namespace Monopoly.Tests.TestDoubles;

internal sealed class ScriptedMatchRandomSource : IMatchRandomSource
{
    private readonly Queue<int> _values;
    private readonly List<RandomRequest> _requests = [];
    private readonly ReadOnlyCollection<RandomRequest> _requestsView;
    private readonly bool _useMinimumForDeckShuffle;

    internal ScriptedMatchRandomSource(params int[] values)
        : this(values, false)
    {
    }

    private ScriptedMatchRandomSource(IEnumerable<int> values, bool useMinimumForDeckShuffle)
    {
        _values = new Queue<int>(values ?? throw new ArgumentNullException(nameof(values)));
        _requestsView = _requests.AsReadOnly();
        _useMinimumForDeckShuffle = useMinimumForDeckShuffle;
    }

    internal static ScriptedMatchRandomSource ForDice(params int[] values) => new(values, true);

    internal IReadOnlyList<RandomRequest> Requests => _requestsView;
    internal int RemainingCount => _values.Count;

    public int NextInt(RandomRequest request)
    {
        _requests.Add(request);
        if (_useMinimumForDeckShuffle && request.Purpose == RandomPurpose.DeckShuffle)
            return request.MinimumInclusive;
        if (_values.Count == 0)
        {
            throw new RandomSourceException(
                RandomSourceErrorKind.Exhausted,
                request,
                $"The scripted random source has no value for {request.Purpose} at sequence index {request.SequenceIndex}.");
        }

        return _values.Dequeue();
    }
}

internal sealed class MinimumMatchRandomSource : IMatchRandomSource
{
    private readonly List<RandomRequest> _requests = [];
    private readonly ReadOnlyCollection<RandomRequest> _requestsView;

    internal MinimumMatchRandomSource()
    {
        _requestsView = _requests.AsReadOnly();
    }

    internal IReadOnlyList<RandomRequest> Requests => _requestsView;

    public int NextInt(RandomRequest request)
    {
        _requests.Add(request);
        return request.MinimumInclusive;
    }
}
