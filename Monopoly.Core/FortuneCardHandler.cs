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
        internal Queue<IChanceCard> ChanceQueue { get; set; }
        internal Queue<ICommunityChestCard> CommunityChestQueue { get; set; }

        public FortuneCardHandler(GameRules gameRules)
        {
            InitializeQueues(gameRules);
        }

        internal void InitializeQueues(GameRules gameRules)
        {
            ChanceQueue = new Queue<IChanceCard>(Data.FortuneCardBuilder.GetChanceCards(gameRules));
            CommunityChestQueue = new Queue<ICommunityChestCard>(Data.FortuneCardBuilder.GetCommunityChestCards(gameRules));
            ShuffleQueues();
        }

        internal void ShuffleQueues()
        {
            Random random = new Random();
            ChanceQueue = new Queue<IChanceCard>(ChanceQueue.OrderBy(c => random.Next()));
            CommunityChestQueue = new Queue<ICommunityChestCard>(CommunityChestQueue.OrderBy(c => random.Next()));
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
