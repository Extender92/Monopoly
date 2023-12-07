using Monopoly.Core.Models.FortuneCard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Monopoly.Core
{
    internal class FortuneCardHandler
    {
        private Queue<IChanceCard> ChanceQueue { get; set; }
        private Queue<ICommunityChestCard> CommunityChestQueue { get; set; }
        private Random Random { get; }

        public FortuneCardHandler(GameRules gameRules)
        {
            InitializeQueues(gameRules);
            Random = new Random();
        }

        private void InitializeQueues(GameRules gameRules)
        {
            ChanceQueue = new Queue<IChanceCard>(Data.FortuneCardBuilder.GetChanceCards(gameRules));
            CommunityChestQueue = new Queue<ICommunityChestCard>(Data.FortuneCardBuilder.GetCommunityChestCards(gameRules));
            ShuffleQueues();
        }

        private void ShuffleQueues()
        {
            ChanceQueue = new Queue<IChanceCard>(ChanceQueue.OrderBy(c => Random.Next()));
            CommunityChestQueue = new Queue<ICommunityChestCard>(CommunityChestQueue.OrderBy(c => Random.Next()));
        }

        internal IChanceCard DrawNextChanceCard()
        {
            var drawnCard = ChanceQueue.Dequeue();
            ChanceQueue.Enqueue(drawnCard);
            return drawnCard;
        }

        internal ICommunityChestCard DrawNextCommunityChestCard()
        {
            var drawnCard = CommunityChestQueue.Dequeue();
            CommunityChestQueue.Enqueue(drawnCard);
            return drawnCard;
        }
    }
}
