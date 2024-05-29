using Monopoly.Core.Models;
using Monopoly.Console.Utilities;
using Monopoly.Core.Interface;
using System;
using System.Numerics;


namespace Monopoly.Console.GUI
{
    internal class PlayerActionMenu
    {
        private readonly IGame CurrentGame;
        private readonly IMenuOptionSelector MenuOptionSelector;
        private readonly Player Player;

        public PlayerActionMenu(IMenuOptionSelector menuOptionSelector, IGame game, Player player)
        {
            MenuOptionSelector = menuOptionSelector;
            CurrentGame = game;
            Player = player;
        }

        public enum PlayerActionMainMenu
        {
            [DisplayName("Roll Dice")]
            RollDice,

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
            BackToActionMenu
        }

        public void DisplayPlayerActionMainMenu()
        {
            List<PlayerActionMainMenu> availableActions = GetAvailableMainActions();
            int selectedOption = MenuOptionSelector.GetSelectedOption(availableActions.Select(action => action.GetDisplayName()).ToList());

            HandlePlayerActionMainMenu(availableActions[selectedOption]);
        }

        public void DisplayPlayerActionRealEstateMenu()
        {
            List<PlayerActionRealEstateMenu> availableActions = GetAvailableRealEstateActions();
            int selectedOption = MenuOptionSelector.GetSelectedOption(availableActions.Select(action => action.GetDisplayName()).ToList());

            HandlePlayerActionRealEstateMenu(availableActions[selectedOption]);
        }

        private List<PlayerActionMainMenu> GetAvailableMainActions()
        {
            List<PlayerActionMainMenu> actions = new List<PlayerActionMainMenu>
            {
                PlayerActionMainMenu.RollDice,
                PlayerActionMainMenu.RealEstateMenu,
                PlayerActionMainMenu.SaveMenu,
                PlayerActionMainMenu.ExitToMainMenu
            };

            return actions;
        }

        private List<PlayerActionRealEstateMenu> GetAvailableRealEstateActions()
        {
            List<PlayerActionRealEstateMenu> actions = new List<PlayerActionRealEstateMenu>
            {
                PlayerActionRealEstateMenu.ManageHouses,
                PlayerActionRealEstateMenu.MortgageProperties,
                PlayerActionRealEstateMenu.BackToActionMenu
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
                case PlayerActionMainMenu.RealEstateMenu:
                    DisplayPlayerActionRealEstateMenu();
                    break;
                case PlayerActionMainMenu.SaveMenu:
                    SaveGame();
                    break;
                case PlayerActionMainMenu.ExitToMainMenu:
                    ExitToMainMenu();
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
                    new HouseMenu(MenuOptionSelector, CurrentGame, Player).DisplayHouseMenu();
                    break;
                case PlayerActionRealEstateMenu.MortgageProperties:
                    new MortgageMenu(MenuOptionSelector, CurrentGame, Player).DisplayMortgageMenu();
                    break;
                case PlayerActionRealEstateMenu.BackToActionMenu:
                    DisplayPlayerActionMainMenu();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), $"Invalid value for 'selectedOption': {action}");
            }
        }

        private void RollDice()
        {
            CurrentGame.Handler.RoleDiceAndMovePlayer(Player);
        }

        private void SaveGame()
        {
            // Logic to save the game
            DisplayPlayerActionMainMenu();

        }

        private void ExitToMainMenu()
        {
            // Logic to exit to main menu
            DisplayPlayerActionMainMenu();
        }
    }
}