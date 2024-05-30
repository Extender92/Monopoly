using Monopoly.Console.Utilities;
using Monopoly.Core.Interface;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Numerics;

namespace Monopoly.Console.GUI
{
    internal class HouseMenu
    {
        private readonly IGame CurrentGame;
        private readonly IMenuOptionSelector MenuOptionSelector;
        private readonly ListOptionSelector ListOptionSelector;
        private readonly Player Player;
        private List<PropertySquare> AvailableBuyHouseList;
        private List<PropertySquare> AvailableSellHouseList;

        int SelectedOption = 0;

        public HouseMenu(IMenuOptionSelector menuOptionSelector, IGame game, Player player)
        {
            MenuOptionSelector = menuOptionSelector;
            ListOptionSelector = new ListOptionSelector();
            CurrentGame = game;
            Player = player;
            UpdateLists();
        }

        public void UpdateLists()
        {
            AvailableBuyHouseList = CurrentGame.Board.GetAllPropertySquaresPlayerCanBuyHousesIn(Player) ?? new List<PropertySquare>();
            AvailableSellHouseList = CurrentGame.Board.GetAllPropertySquaresPlayerCanSellHousesIn(Player) ?? new List<PropertySquare>();
        }

        public enum HouseMenuOptions
        {
            [DisplayName("Buy House")]
            BuyHouse,
            [DisplayName("Buy House - No Properties Available")]
            BuyHousePlaceHolder,

            [DisplayName("Sell House")]
            SellHouse,
            [DisplayName("Sell House - No Properties Available")]
            SellHousePlaceHolder,

            [DisplayName("Back To Real Estate Menu")]
            BackToRealEstateMenu
        }

        public void DisplayHouseMenu()
        {
            List<HouseMenuOptions> availableActions = GetAvailableHouseActions();
            SelectedOption = MenuOptionSelector.GetSelectedOption(availableActions.Select(action => action.GetDisplayName()).ToList(), 18, SelectedOption);

            HandleHouseMenu(availableActions[SelectedOption]);
        }

        private List<HouseMenuOptions> GetAvailableHouseActions()
        {
            var actions = new List<HouseMenuOptions>
            {
                AvailableBuyHouseList.Any() ? HouseMenuOptions.BuyHouse : HouseMenuOptions.BuyHousePlaceHolder,
                AvailableSellHouseList.Any() ? HouseMenuOptions.SellHouse : HouseMenuOptions.SellHousePlaceHolder,
                HouseMenuOptions.BackToRealEstateMenu
            };

            return actions;
        }

        private void HandleHouseMenu(HouseMenuOptions action)
        {
            switch (action)
            {
                case HouseMenuOptions.BuyHouse:
                    BuyHouse();
                    break;
                case HouseMenuOptions.BuyHousePlaceHolder:
                    StayOnCurrentMenu();
                    break;
                case HouseMenuOptions.SellHouse:
                    SellHouse();
                    break;
                case HouseMenuOptions.SellHousePlaceHolder:
                    StayOnCurrentMenu();
                    break;
                case HouseMenuOptions.BackToRealEstateMenu:
                    new PlayerActionMenu(CurrentGame, Player).DisplayPlayerActionRealEstateMenu();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), $"Invalid value for 'selectedOption': {action}");
            }
        }

        private void StayOnCurrentMenu()
        {
            DisplayHouseMenu();
        }

        private void BuyHouse()
        {
            int index = 0;
            int spacingPerLine = AvailableBuyHouseList.Max(x => x.Name.Length);
            int optionsPerLine = AvailableBuyHouseList.Count / 2;
            string errorMessage = "";

            bool canBuy = false;
            do
            {
                index = ListOptionSelector.GetSelectedOption(AvailableBuyHouseList.Cast<Square>().ToList(), spacingPerLine, index, errorMessage, optionsPerLine, true);
                if (index == -1) return;

                errorMessage = "";
                var selectedProperty = AvailableBuyHouseList[index];

                if (selectedProperty.Houses < 5) canBuy = true;
                else errorMessage = ($"Cannot buy more Houses or Hotels on {selectedProperty.Name}. It already has {selectedProperty.GetHouseCountAsString()}.");

            } while (!canBuy);
            CurrentGame.Transactions.BuyPropertyHouse(Player, AvailableBuyHouseList[index]);
            UpdateLists();
            StayOnCurrentMenu();
        }

        private void SellHouse()
        {
            int index = 0;
            int spacingPerLine = AvailableSellHouseList.Max(x => x.Name.Length);
            int optionsPerLine = AvailableSellHouseList.Count / 2;
            string errorMessage = "";

            bool canSell = false;
            do
            {
                index = ListOptionSelector.GetSelectedOption(AvailableSellHouseList.Cast<Square>().ToList(), spacingPerLine, index, errorMessage, optionsPerLine, true);
                if (index == -1) return;

                errorMessage = "";
                var selectedProperty = AvailableSellHouseList[index];

                if (selectedProperty.Houses > 0) canSell = true;
                else errorMessage = ($"Cannot sell Houses or Hotels on {selectedProperty.Name}. It has {selectedProperty.GetHouseCountAsString()}.");

            } while (!canSell);
            CurrentGame.Transactions.SellPropertyHouse(Player, AvailableSellHouseList[index]);
            UpdateLists();
            StayOnCurrentMenu();
        }
    }
}