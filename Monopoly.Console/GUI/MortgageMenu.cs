using Monopoly.Console.Utilities;
using Monopoly.Core.Interface;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.GUI
{
    internal class MortgageMenu
    {
        private readonly IGame CurrentGame;
        private readonly IMenuOptionSelector MenuOptionSelector;
        private readonly Player Player;
        private readonly List<Square> AvailableMortgageList;
        private readonly List<Square> AvailableLiftMortgageList;

        int SelectedOption = 0;

        public MortgageMenu(IMenuOptionSelector menuOptionSelector, IGame game, Player player)
        {
            MenuOptionSelector = menuOptionSelector;
            CurrentGame = game;
            Player = player;
            AvailableMortgageList = CurrentGame.Board.GetPlayerUnmortgagedSquares(player) ?? new List<Square>();
            AvailableLiftMortgageList = CurrentGame.Board.GetPlayerMortgagedSquares(player) ?? new List<Square>();
        }

        public enum MortgageMenuOptions
        {
            [DisplayName("Mortgage Property")]
            MortgageProperty,
            [DisplayName("Mortgage Property - No Properties Available")]
            MortgagePropertyPlaceHolder,

            [DisplayName("Lift Mortgage")]
            LiftMortgage,
            [DisplayName("Lift Mortgage - No Properties Available")]
            LiftMortgagePlaceHolder,

            [DisplayName("Back To Real Estate Menu")]
            BackToRealEstateMenu
        }

        public void DisplayMortgageMenu()
        {
            List<MortgageMenuOptions> availableActions = GetAvailableMortgageActions();
            SelectedOption = MenuOptionSelector.GetSelectedOption(availableActions.Select(action => action.GetDisplayName()).ToList(), 18, SelectedOption);

            HandleMortgageMenu(availableActions[SelectedOption]);
        }

        private List<MortgageMenuOptions> GetAvailableMortgageActions()
        {
            var actions = new List<MortgageMenuOptions>
            {
                AvailableMortgageList.Any() ? MortgageMenuOptions.MortgageProperty : MortgageMenuOptions.MortgagePropertyPlaceHolder,
                AvailableLiftMortgageList.Any() ? MortgageMenuOptions.LiftMortgage : MortgageMenuOptions.LiftMortgagePlaceHolder,
                MortgageMenuOptions.BackToRealEstateMenu
            };

            return actions;
        }

        private void HandleMortgageMenu(MortgageMenuOptions action)
        {
            switch (action)
            {
                case MortgageMenuOptions.MortgageProperty:
                    MortgageProperty();
                    break;
                case MortgageMenuOptions.MortgagePropertyPlaceHolder:
                    // Do nothing and stay on the current menu
                    StayOnCurrentMenu();
                    break;
                case MortgageMenuOptions.LiftMortgage:
                    LiftMortgage();
                    break;
                case MortgageMenuOptions.LiftMortgagePlaceHolder:
                    // Do nothing and stay on the current menu
                    StayOnCurrentMenu();
                    break;
                case MortgageMenuOptions.BackToRealEstateMenu:
                    new PlayerActionMenu(MenuOptionSelector, CurrentGame, Player).DisplayPlayerActionRealEstateMenu();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), $"Invalid value for 'selectedOption': {action}");
            }
        }

        private void StayOnCurrentMenu()
        {
            DisplayMortgageMenu();
        }

        private void MortgageProperty()
        {
            DisplayMortgageMenu();
        }

        private void LiftMortgage()
        {
            DisplayMortgageMenu();
        }
    }
}