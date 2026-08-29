using Monopoly.Core.Models.FortuneCard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

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

        internal FortuneCardHandler(GameRules gameRules)
        {
            InitializeQueues(gameRules);
        }

        private void InitializeQueues(GameRules gameRules)
        {
            _chanceCards = Data.FortuneCardBuilder.GetChanceCards(gameRules);
            _communityChestCards = Data.FortuneCardBuilder.GetCommunityChestCards(gameRules);
            _chanceQueue = new Queue<IChanceCard>(_chanceCards);
            _communityChestQueue = new Queue<ICommunityChestCard>(_communityChestCards);
            ShuffleQueues();
        }

        internal void ShuffleQueues()
        {
            Random random = new Random();
            _chanceQueue = new Queue<IChanceCard>(_chanceQueue.OrderBy(c => random.Next()));
            _communityChestQueue = new Queue<ICommunityChestCard>(_communityChestQueue.OrderBy(c => random.Next()));
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
    }
}
