using Monopoly.Console.Utilities;
using Monopoly.Core.Interface;
using Monopoly.Core.Models.Board;
using Monopoly.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly.Console.GUI
{
    internal class HouseMenu
    {
        private readonly IGame CurrentGame;
        private readonly IMenuOptionSelector MenuOptionSelector;
        private readonly Player Player;
        private readonly List<PropertySquare> AvailableBuyHouseList;
        private readonly List<PropertySquare> AvailableSellHouseList;

        int SelectedOption = 0;

        public HouseMenu(IMenuOptionSelector menuOptionSelector, IGame game, Player player)
        {
            MenuOptionSelector = menuOptionSelector;
            CurrentGame = game;
            Player = player;
            AvailableBuyHouseList = CurrentGame.Board.GetAllPropertySquaresPlayerCanBuyHousesIn(player) ?? new List<PropertySquare>();
            AvailableSellHouseList = CurrentGame.Board.GetAllPropertySquaresPlayerCanSellHousesIn(player) ?? new List<PropertySquare>();
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
                    // Do nothing and stay on the current menu
                    StayOnCurrentMenu();
                    break;
                case HouseMenuOptions.SellHouse:
                    SellHouse();
                    break;
                case HouseMenuOptions.SellHousePlaceHolder:
                    // Do nothing and stay on the current menu
                    StayOnCurrentMenu();
                    break;
                case HouseMenuOptions.BackToRealEstateMenu:
                    new PlayerActionMenu(MenuOptionSelector, CurrentGame, Player).DisplayPlayerActionRealEstateMenu();
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
            DisplayHouseMenu();
        }

        private void SellHouse()
        {
            DisplayHouseMenu();
        }
    }
}