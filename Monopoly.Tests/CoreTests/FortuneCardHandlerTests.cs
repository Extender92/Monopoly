using Monopoly.Core.Data;
using Monopoly.Core.Models.FortuneCard;
using Monopoly.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Tests.CoreTests
{
    public class FortuneCardHandlerTests
    {
        [Fact]
        public void InitializeQueues_ShouldInitializeQueuesAndShuffle()
        {
            // Arrange
            var gameRules = new GameRules(numberOfPlayers: 4, numberOfDice: 2, dieSides: 6);
            var fortuneCardHandler = new FortuneCardHandler(gameRules);

            // Check if the queues are shuffled by comparing the order before and after shuffle
            var originalChanceOrder = fortuneCardHandler.ChanceQueue.Select(c => c.Info).ToList();
            var originalCommunityChestOrder = fortuneCardHandler.CommunityChestQueue.Select(c => c.Info).ToList();

            // Act
            fortuneCardHandler.InitializeQueues(gameRules);

            fortuneCardHandler.ShuffleQueues();

            // Assert
            Assert.NotNull(fortuneCardHandler.ChanceQueue);
            Assert.Equal(FortuneCardBuilder.GetChanceCards(gameRules).Count, fortuneCardHandler.ChanceQueue.Count);
            Assert.NotNull(fortuneCardHandler.CommunityChestQueue);
            Assert.Equal(FortuneCardBuilder.GetCommunityChestCards(gameRules).Count, fortuneCardHandler.CommunityChestQueue.Count);

            var shuffledChanceOrder = fortuneCardHandler.ChanceQueue.Select(c => c.Info).ToList();
            var shuffledCommunityChestOrder = fortuneCardHandler.CommunityChestQueue.Select(c => c.Info).ToList();

            Assert.NotEqual(originalChanceOrder, shuffledChanceOrder);
            Assert.NotEqual(originalCommunityChestOrder, shuffledCommunityChestOrder);
        }

        [Fact]
        public void DrawNextChanceCard_ShouldReturnNextCardAndEnqueue()
        {
            // Arrange
            var gameRules = new GameRules(numberOfPlayers: 4, numberOfDice: 2, dieSides: 6);
            var fortuneCardHandler = new FortuneCardHandler(gameRules);

            // Act
            var drawnCard = fortuneCardHandler.DrawNextChanceCard();

            // Assert
            Assert.NotNull(drawnCard);
            Assert.IsAssignableFrom<IChanceCard>(drawnCard);

            // Check if the first card is not the same card in queue
            var nextCard = fortuneCardHandler.ChanceQueue.Peek();
            Assert.NotEqual(drawnCard, nextCard);

            // Check if the drawn card is enqueued back to the end of the queue
            var cardsAfterDraw = fortuneCardHandler.ChanceQueue.ToList();
            Assert.Equal(drawnCard, cardsAfterDraw.Last());
        }

        [Fact]
        public void DrawNextCommunityChestCard_ShouldReturnNextCardAndEnqueue()
        {
            // Arrange
            var gameRules = new GameRules(numberOfPlayers: 4, numberOfDice: 2, dieSides: 6);
            var fortuneCardHandler = new FortuneCardHandler(gameRules);

            // Act
            var drawnCard = fortuneCardHandler.DrawNextCommunityChestCard();

            // Assert
            Assert.NotNull(drawnCard);
            Assert.IsAssignableFrom<ICommunityChestCard>(drawnCard);

            // Check if the first card is not the same card in queue
            var nextCard = fortuneCardHandler.CommunityChestQueue.Peek();
            Assert.NotEqual(drawnCard, nextCard);

            // Check if the drawn card is enqueued back to the end of the queue
            var cardsAfterDraw = fortuneCardHandler.CommunityChestQueue.ToList();
            Assert.Equal(drawnCard, cardsAfterDraw.Last());
        }
    }
}
