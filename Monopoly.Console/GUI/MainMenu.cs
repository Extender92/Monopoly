﻿using Monopoly.Console.Utilities;
using Monopoly.Core.Interface;
using Monopoly.Core.Models;
using Monopoly.Core.SaveAndLoad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.GUI
{
    internal class MainMenu
    {
        private readonly IMenuOptionSelector MenuOptionSelector;

        public MainMenu(IMenuOptionSelector menuOptionSelector)
        {
            MenuOptionSelector = menuOptionSelector;
        }

        public enum MainMenuOptions
        {
            [DisplayName("Start New Game")]
            StartNewGame,

            [DisplayName("Load Game")]
            LoadGame,

            [DisplayName("Exit Game")]
            ExitGame,

            [DisplayName("Back To Game")]
            BackToGame,
        }

        public enum StartNewGameMenu
        {
            [DisplayName("Start Game")]
            StartGame,

            [DisplayName("Setup Rules")]
            SetupRules,

            [DisplayName("Back")]
            BackToMainMenu,
        }

        public void DisplayMainMenu()
        {
            List<MainMenuOptions> availableActions = GetAvailableMainActions();
            int selectedOption = MenuOptionSelector.GetSelectedOption(availableActions.Select(action => action.GetDisplayName()).ToList());

            HandleActionMainMenu(availableActions[selectedOption]);
        }

        public void DisplayStartNewGameMenu()
        {
            List<StartNewGameMenu> availableActions = GetAvailableRealEstateActions();
            int selectedOption = MenuOptionSelector.GetSelectedOption(availableActions.Select(action => action.GetDisplayName()).ToList());

            HandlePlayerActionRealEstateMenu(availableActions[selectedOption]);
        }

        private List<MainMenuOptions> GetAvailableMainActions()
        {
            List<MainMenuOptions> actions = new List<MainMenuOptions>
            {
                MainMenuOptions.StartNewGame,
                MainMenuOptions.LoadGame,
                // CurrentGame ? MainMenuOptions.BackToGame,
                MainMenuOptions.ExitGame,
            };

            return actions;
        }

        private List<StartNewGameMenu> GetAvailableRealEstateActions()
        {
            List<StartNewGameMenu> actions = new List<StartNewGameMenu>
            {
                StartNewGameMenu.StartGame,
                StartNewGameMenu.SetupRules,
                StartNewGameMenu.BackToMainMenu
            };

            return actions;
        }

        private void HandleActionMainMenu(MainMenuOptions action)
        {
            switch (action)
            {
                case MainMenuOptions.StartNewGame:
                    DisplayStartNewGameMenu();
                    break;
                case MainMenuOptions.LoadGame:
                    LoadGame();
                    break;
                case MainMenuOptions.ExitGame:
                    Environment.Exit(0);
                    break;
                case MainMenuOptions.BackToGame:
                    // PlayerActionMenu PlayerActionMenu = new PlayerActionMenu(MenuOptionSelector, CurrentGame, player);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), $"Invalid value for 'selectedOption': {action}");
            }
        }

        private void HandlePlayerActionRealEstateMenu(StartNewGameMenu action)
        {
            switch (action)
            {
                case StartNewGameMenu.StartGame:
                    Program.StartNewGame();
                    break;
                case StartNewGameMenu.SetupRules:
                    SetupRules();
                    break;
                case StartNewGameMenu.BackToMainMenu:
                    DisplayMainMenu();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), $"Invalid value for 'selectedOption': {action}");
            }
        }

        private void SetupRules()
        {
            // SetupRules
            DisplayStartNewGameMenu();
        }

        private void LoadGame()
        {
            // Logic to Load the game
            Program.LoadGame();
            DisplayMainMenu();
        }
    }
}
