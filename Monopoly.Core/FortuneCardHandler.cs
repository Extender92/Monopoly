using Monopoly.Core.Models.FortuneCard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Monopoly.Core
{
    public class FortuneCardHandler
    {
        public Queue<IChanceCard> ChanceQueue { get; set; } = new();
        public Queue<ICommunityChestCard> CommunityChestQueue { get; set; } = new();
        private List<IChanceCard> ChanceCards { get; set; } = new();
        private List<ICommunityChestCard> CommunityChestCards { get; set; } = new();

        public FortuneCardHandler(GameRules gameRules)
        {
            InitializeQueues(gameRules);
        }

        public void InitializeQueues(GameRules gameRules)
        {
            ChanceCards = Data.FortuneCardBuilder.GetChanceCards(gameRules);
            CommunityChestCards = Data.FortuneCardBuilder.GetCommunityChestCards(gameRules);
            ChanceQueue = new Queue<IChanceCard>(ChanceCards);
            CommunityChestQueue = new Queue<ICommunityChestCard>(CommunityChestCards);
            ShuffleQueues();
        }

        public void ShuffleQueues()
        {
            Random random = new Random();
            ChanceQueue = new Queue<IChanceCard>(ChanceQueue.OrderBy(c => random.Next()));
            CommunityChestQueue = new Queue<ICommunityChestCard>(CommunityChestQueue.OrderBy(c => random.Next()));
        }

        public IChanceCard DrawNextChanceCard()
        {
            var drawnCard = ChanceQueue.Dequeue();
            ChanceQueue.Enqueue(drawnCard);
            return drawnCard;
        }

        public ICommunityChestCard DrawNextCommunityChestCard()
        {
            var drawnCard = CommunityChestQueue.Dequeue();
            CommunityChestQueue.Enqueue(drawnCard);
            return drawnCard;
        }

        internal IReadOnlyList<string> GetChanceDeckOrder() => ChanceQueue.Select(card => ChanceCards.IndexOf(card).ToString()).ToList();

        internal IReadOnlyList<string> GetCommunityChestDeckOrder() => CommunityChestQueue.Select(card => CommunityChestCards.IndexOf(card).ToString()).ToList();

        internal void RestoreDeckOrder(IReadOnlyList<string> chanceOrder, IReadOnlyList<string> communityChestOrder)
        {
            ChanceQueue = RestoreQueue(ChanceQueue, chanceOrder, ChanceCards);
            CommunityChestQueue = RestoreQueue(CommunityChestQueue, communityChestOrder, CommunityChestCards);
        }

        private static Queue<T> RestoreQueue<T>(IEnumerable<T> source, IReadOnlyList<string> order, IReadOnlyList<T> canonicalCards)
            where T : class
        {
            List<T> remaining = source.ToList();
            List<T> restored = new();
            foreach (string key in order)
            {
                if (!int.TryParse(key, out int index) || index < 0 || index >= canonicalCards.Count)
                    continue;

                T card = canonicalCards[index];
                if (!remaining.Remove(card)) continue;
                restored.Add(card);
            }

            restored.AddRange(remaining);
            return new Queue<T>(restored);
        }
    }
}
