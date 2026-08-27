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
        public void ConstructorInitializesCompleteReadOnlyDecks()
        {
            // Arrange
            var gameRules = new GameRules(numberOfPlayers: 4, numberOfDice: 2, dieSides: 6);
            var fortuneCardHandler = new FortuneCardHandler(gameRules);

            // Assert
            Assert.Equal(FortuneCardBuilder.GetChanceCards(gameRules).Count, fortuneCardHandler.ChanceDeck.Count);
            Assert.Equal(FortuneCardBuilder.GetCommunityChestCards(gameRules).Count, fortuneCardHandler.CommunityChestDeck.Count);
            Assert.Throws<NotSupportedException>(() => ((IList<IFortuneCardView>)fortuneCardHandler.ChanceDeck).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<IFortuneCardView>)fortuneCardHandler.CommunityChestDeck).Clear());
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
            var nextCard = fortuneCardHandler.ChanceDeck[0];
            Assert.NotEqual(drawnCard, nextCard);

            // Check if the drawn card is enqueued back to the end of the queue
            var cardsAfterDraw = fortuneCardHandler.ChanceDeck;
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
            var nextCard = fortuneCardHandler.CommunityChestDeck[0];
            Assert.NotEqual(drawnCard, nextCard);

            // Check if the drawn card is enqueued back to the end of the queue
            var cardsAfterDraw = fortuneCardHandler.CommunityChestDeck;
            Assert.Equal(drawnCard, cardsAfterDraw.Last());
        }
    }
}
