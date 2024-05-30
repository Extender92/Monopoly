using Monopoly.Console.Utilities;
using Monopoly.Core.Interface;
using Monopoly.Core.Models;
using Monopoly.Core.Models.Board;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.GUI
{
    internal class MortgageMenu
    {
        private readonly IGame CurrentGame;
        private readonly IMenuOptionSelector MenuOptionSelector;
        private readonly ListOptionSelector ListOptionSelector;
        private readonly Player Player;
        private List<Square> AvailableMortgageList;
        private List<Square> AvailableLiftMortgageList;

        int SelectedOption = 0;

        public MortgageMenu(IMenuOptionSelector menuOptionSelector, IGame game, Player player)
        {
            MenuOptionSelector = menuOptionSelector;
            ListOptionSelector = new ListOptionSelector();
            CurrentGame = game;
            Player = player;
            UpdateLists();
        }

        public void UpdateLists()
        {
            AvailableMortgageList = CurrentGame.Board.GetPlayerUnmortgagedSquares(Player) ?? new List<Square>();
            AvailableLiftMortgageList = CurrentGame.Board.GetPlayerMortgagedSquares(Player) ?? new List<Square>();
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
                    new PlayerActionMenu(CurrentGame, Player).DisplayPlayerActionRealEstateMenu();
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
            int index = 0;
            int spacingPerLine = AvailableMortgageList.Max(x => x.Name.Length);
            int optionsPerLine = AvailableMortgageList.Count / 2;
            string errorMessage = "";

            bool canMortgage = false;
            do
            {
                index = ListOptionSelector.GetSelectedOption(AvailableMortgageList.Cast<Square>().ToList(), spacingPerLine, index, errorMessage, optionsPerLine, true);
                if (index == -1) return;

                errorMessage = "";
                var selectedSquare = AvailableMortgageList[index];

                if (!selectedSquare.IsMortgage) canMortgage = true;
                else errorMessage = $"Cannot mortgage property on {selectedSquare.Name}. It is already mortgage.";

            } while (!canMortgage);
            CurrentGame.Transactions.MortgageProperty(Player, AvailableMortgageList[index]);
            UpdateLists();
            StayOnCurrentMenu();
        }

        private void LiftMortgage()
        {
            int index = 0;
            int spacingPerLine = AvailableLiftMortgageList.Max(x => x.Name.Length);
            int optionsPerLine = AvailableLiftMortgageList.Count / 2;
            string errorMessage = "";

            bool canLift = false;
            do
            {
                index = ListOptionSelector.GetSelectedOption(AvailableLiftMortgageList.Cast<Square>().ToList(), spacingPerLine, index, errorMessage, optionsPerLine, true);
                if (index == -1) return;

                errorMessage = "";
                var selectedSquare = AvailableLiftMortgageList[index];

                if (selectedSquare.IsMortgage) canLift = true;
                else errorMessage = $"Cannot lift mortgage on {selectedSquare.Name}. It is not mortgage.";

            } while (!canLift);
            CurrentGame.Transactions.RepayMortgageProperty(Player, AvailableLiftMortgageList[index]);
            UpdateLists();
            StayOnCurrentMenu();
        }
    }
}