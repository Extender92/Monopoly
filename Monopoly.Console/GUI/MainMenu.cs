using Monopoly.Console.Utilities;
using Monopoly.Core;
using Monopoly.Core.Persistence;

namespace Monopoly.Console.GUI;

internal sealed class MainMenu
{
    private readonly IMenuOptionSelector _menuOptionSelector;
    private readonly IGameSaveStore _saveStore;
    private readonly ValidatedGameProfile _profile;

    internal MainMenu(
        IMenuOptionSelector menuOptionSelector,
        IGameSaveStore saveStore,
        ValidatedGameProfile profile)
    {
        _menuOptionSelector = menuOptionSelector ?? throw new ArgumentNullException(nameof(menuOptionSelector));
        _saveStore = saveStore ?? throw new ArgumentNullException(nameof(saveStore));
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    private enum MainMenuOption
    {
        [DisplayName("Start New Game")]
        StartNewGame,

        [DisplayName("Load Game")]
        LoadGame,

        [DisplayName("Exit Game")]
        ExitGame
    }

    internal void DisplayMainMenu()
    {
        MainMenuOption[] actions = Enum.GetValues<MainMenuOption>();
        while (true)
        {
            int selectedIndex = _menuOptionSelector.GetSelectedOption(
                actions.Select(action => action.GetDisplayName()).ToList());

            switch (actions[selectedIndex])
            {
                case MainMenuOption.StartNewGame:
                    Program.StartNewGame(_saveStore, _profile);
                    break;
                case MainMenuOption.LoadGame:
                    Program.LoadGame(_saveStore);
                    break;
                case MainMenuOption.ExitGame:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(selectedIndex));
            }
        }
    }
}
