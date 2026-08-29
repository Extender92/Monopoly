using Monopoly.Core.Models.FortuneCard;
using Monopoly.Core.Randomness;

namespace Monopoly.Core
{
    public sealed class FortuneCardHandler
    {
        private Queue<IChanceCard> _chanceQueue = new();
        private Queue<ICommunityChestCard> _communityChestQueue = new();
        private List<IChanceCard> _chanceCards = new();
        private List<ICommunityChestCard> _communityChestCards = new();
        public IReadOnlyList<IFortuneCardView> ChanceDeck =>
            Array.AsReadOnly(_chanceQueue.Cast<IFortuneCardView>().ToArray());
        public IReadOnlyList<IFortuneCardView> CommunityChestDeck =>
            Array.AsReadOnly(_communityChestQueue.Cast<IFortuneCardView>().ToArray());
        public Presentation.PresentationToken ChanceDeckPresentationToken => Presentation.PresentationTokens.PrimaryDeck;
        public Presentation.PresentationToken CommunityChestDeckPresentationToken => Presentation.PresentationTokens.SecondaryDeck;
        internal IReadOnlyList<Presentation.PresentationMetadata> AllPresentationMetadata =>
            Array.AsReadOnly(_chanceCards.Select(card => card.Presentation)
                .Concat(_communityChestCards.Select(card => card.Presentation))
                .ToArray());

        internal FortuneCardHandler(GameRules gameRules, MatchRandomizer randomizer, bool shuffleDecks)
        {
            InitializeQueues(gameRules, randomizer, shuffleDecks);
        }

        private void InitializeQueues(GameRules gameRules, MatchRandomizer randomizer, bool shuffleDecks)
        {
            ArgumentNullException.ThrowIfNull(gameRules);
            ArgumentNullException.ThrowIfNull(randomizer);
            _chanceCards = Data.FortuneCardBuilder.GetChanceCards(gameRules);
            _communityChestCards = Data.FortuneCardBuilder.GetCommunityChestCards(gameRules);
            int sequenceIndex = 0;
            IReadOnlyList<IChanceCard> chanceOrder = shuffleDecks
                ? ShuffleCopy(_chanceCards, randomizer, ref sequenceIndex)
                : _chanceCards.ToArray();
            IReadOnlyList<ICommunityChestCard> communityChestOrder = shuffleDecks
                ? ShuffleCopy(_communityChestCards, randomizer, ref sequenceIndex)
                : _communityChestCards.ToArray();

            _chanceQueue = new Queue<IChanceCard>(chanceOrder);
            _communityChestQueue = new Queue<ICommunityChestCard>(communityChestOrder);
        }

        internal IChanceCard DrawNextChanceCard()
        {
            var drawnCard = _chanceQueue.Dequeue();
            _chanceQueue.Enqueue(drawnCard);
            return drawnCard;
        }

        internal ICommunityChestCard DrawNextCommunityChestCard()
        {
            var drawnCard = _communityChestQueue.Dequeue();
            _communityChestQueue.Enqueue(drawnCard);
            return drawnCard;
        }

        internal IReadOnlyList<string> GetChanceDeckOrder() => _chanceQueue.Select(card => _chanceCards.IndexOf(card).ToString()).ToList();

        internal IReadOnlyList<string> GetCommunityChestDeckOrder() => _communityChestQueue.Select(card => _communityChestCards.IndexOf(card).ToString()).ToList();

        internal void RestoreDeckOrder(IReadOnlyList<string> chanceOrder, IReadOnlyList<string> communityChestOrder)
        {
            ArgumentNullException.ThrowIfNull(chanceOrder);
            ArgumentNullException.ThrowIfNull(communityChestOrder);
            Queue<IChanceCard> restoredChanceQueue = RestoreQueue(chanceOrder, _chanceCards);
            Queue<ICommunityChestCard> restoredCommunityChestQueue =
                RestoreQueue(communityChestOrder, _communityChestCards);

            _chanceQueue = restoredChanceQueue;
            _communityChestQueue = restoredCommunityChestQueue;
        }

        private static Queue<T> RestoreQueue<T>(IReadOnlyList<string> order, IReadOnlyList<T> canonicalCards)
            where T : class
        {
            if (order.Count != canonicalCards.Count ||
                order.Any(key => !int.TryParse(key, out int index) || index < 0 || index >= canonicalCards.Count) ||
                order.Distinct().Count() != order.Count)
                throw new ArgumentException("Card order must contain every card exactly once.", nameof(order));

            return new Queue<T>(order.Select(key => canonicalCards[int.Parse(key)]));
        }

        private static IReadOnlyList<T> ShuffleCopy<T>(
            IReadOnlyList<T> source,
            MatchRandomizer randomizer,
            ref int sequenceIndex)
        {
            T[] shuffled = source.ToArray();
            for (int index = shuffled.Length - 1; index > 0; index--)
            {
                int selectedIndex = randomizer.NextInt(new RandomRequest(
                    RandomPurpose.DeckShuffle,
                    0,
                    index + 1,
                    sequenceIndex++));
                (shuffled[index], shuffled[selectedIndex]) = (shuffled[selectedIndex], shuffled[index]);
            }

            return Array.AsReadOnly(shuffled);
        }
    }
}
