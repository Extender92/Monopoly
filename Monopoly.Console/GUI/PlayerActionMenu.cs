using Monopoly.Core.Models;
using Monopoly.Console.Utilities;
using Monopoly.Core;
using Monopoly.Core.Persistence;
using System;
using System.Numerics;


namespace Monopoly.Console.GUI
{
    internal class PlayerActionMenu
    {
        private readonly Game CurrentGame;
        private readonly IMenuOptionSelector MenuOptionSelector;
        private readonly Player Player;
        private readonly IGameSaveStore SaveStore;
        private readonly IConsoleWrapper ConsoleWrapper;
        internal TurnResult? LastTurnResult { get; private set; }
        internal IGameSaveStore CurrentSaveStore => SaveStore;

        public PlayerActionMenu(Game game, Player player, IGameSaveStore saveStore)
            : this(game, player, saveStore, new ConsoleWrapper())
        {
        }

        internal PlayerActionMenu(Game game, Player player, IGameSaveStore saveStore, IConsoleWrapper consoleWrapper)
        {
            ArgumentNullException.ThrowIfNull(game);
            ArgumentNullException.ThrowIfNull(player);
            ArgumentNullException.ThrowIfNull(saveStore);
            ArgumentNullException.ThrowIfNull(consoleWrapper);

            MenuOptionSelector = new MenuOptionSelector(consoleWrapper);
            CurrentGame = game;
            Player = player;
            SaveStore = saveStore;
            ConsoleWrapper = consoleWrapper;
        }

        public enum PlayerActionMainMenu
        {
            [DisplayName("Roll Dice")]
            RollDice,

            [DisplayName("Roll Dice")]
            RollDiceInJail,

            [DisplayName("Real Estate Menu")]
            RealEstateMenu,

            [DisplayName("Save Menu")]
            SaveMenu,

            [DisplayName("Exit To Main Menu")]
            ExitToMainMenu
        }

        public enum PlayerActionRealEstateMenu
        {
            [DisplayName("Manage Houses Menu")]
            ManageHouses,

            [DisplayName("Mortgage Property Menu")]
            MortgageProperties,

            [DisplayName("Back")]
            BackToActionMenu,

            [DisplayName("Back")]
            BackToEvent
        }

        public void DisplayPlayerActionMainMenu()
        {
            List<PlayerActionMainMenu> availableActions = GetAvailableMainActions();
            int selectedOption = MenuOptionSelector.GetSelectedOption(availableActions.Select(action => action.GetDisplayName()).ToList());

            HandlePlayerActionMainMenu(availableActions[selectedOption]);
        }

        public void DisplayPlayerActionRealEstateMenu(bool eventCall = false)
        {
            List<PlayerActionRealEstateMenu> availableActions = GetAvailableRealEstateActions(eventCall);
            int selectedOption = MenuOptionSelector.GetSelectedOption(availableActions.Select(action => action.GetDisplayName()).ToList());

            HandlePlayerActionRealEstateMenu(availableActions[selectedOption]);
        }

        private List<PlayerActionMainMenu> GetAvailableMainActions()
        {
            List<PlayerActionMainMenu> actions = new List<PlayerActionMainMenu>
            {
                CurrentGame.TheJail.IsPlayerInJail(Player) ? PlayerActionMainMenu.RollDiceInJail : PlayerActionMainMenu.RollDice,
                PlayerActionMainMenu.RealEstateMenu,
                PlayerActionMainMenu.SaveMenu,
                PlayerActionMainMenu.ExitToMainMenu
            };

            return actions;
        }

        private List<PlayerActionRealEstateMenu> GetAvailableRealEstateActions(bool eventCall = false)
        {
            List<PlayerActionRealEstateMenu> actions = new List<PlayerActionRealEstateMenu>
            {
                PlayerActionRealEstateMenu.ManageHouses,
                PlayerActionRealEstateMenu.MortgageProperties,
                eventCall ? PlayerActionRealEstateMenu.BackToEvent : PlayerActionRealEstateMenu.BackToActionMenu
            };

            return actions;
        }

        private void HandlePlayerActionMainMenu(PlayerActionMainMenu action)
        {
            switch (action)
            {
                case PlayerActionMainMenu.RollDice:
                    RollDice();
                    break;
                case PlayerActionMainMenu.RollDiceInJail:
                    RollDiceInJail();
                    break;
                case PlayerActionMainMenu.RealEstateMenu:
                    DisplayPlayerActionRealEstateMenu();
                    break;
                case PlayerActionMainMenu.SaveMenu:
                    SaveGame();
                    break;
                case PlayerActionMainMenu.ExitToMainMenu:
                    new MainMenu(MenuOptionSelector, SaveStore).DisplayMainMenu();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), $"Invalid value for 'selectedOption': {action}");
            }
        }

        private void HandlePlayerActionRealEstateMenu(PlayerActionRealEstateMenu action)
        {
            switch (action)
            {
                case PlayerActionRealEstateMenu.ManageHouses:
                    new HouseMenu(MenuOptionSelector, CurrentGame, Player, SaveStore).DisplayHouseMenu();
                    break;
                case PlayerActionRealEstateMenu.MortgageProperties:
                    new MortgageMenu(MenuOptionSelector, CurrentGame, Player, SaveStore).DisplayMortgageMenu();
                    break;
                case PlayerActionRealEstateMenu.BackToActionMenu:
                    DisplayPlayerActionMainMenu();
                    break;
                case PlayerActionRealEstateMenu.BackToEvent:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), $"Invalid value for 'selectedOption': {action}");
            }
        }

        private void RollDice()
        {
            LastTurnResult = CurrentGame.PlayTurn();
        }

        private void RollDiceInJail()
        {
            LastTurnResult = CurrentGame.PlayTurn();
        }

        private void SaveGame()
        {
            SaveCurrentGame();
            DisplayPlayerActionMainMenu();
        }

        internal void SaveCurrentGame()
        {
            try
            {
                SaveStore.Save(CurrentGame);
            }
            catch (SaveStoreException exception)
            {
                ConsoleWrapper.WriteLine($"The game could not be saved: {exception.Message}");
                ConsoleWrapper.ReadLine();
            }
        }
    }
}
