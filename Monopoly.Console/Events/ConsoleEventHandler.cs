using Monopoly.Core.Notifications;

namespace Monopoly.Console.Events;

internal sealed class ConsoleEventHandler : IDisposable
{
    private readonly ConsoleGame _consoleGame;
    private IDisposable? _subscription;

    private ConsoleEventHandler(ConsoleGame consoleGame)
    {
        _consoleGame = consoleGame ?? throw new ArgumentNullException(nameof(consoleGame));
        _subscription = consoleGame.CurrentGame.Notifications.Subscribe(HandleNotification);
    }

    internal static IDisposable Subscribe(ConsoleGame consoleGame) =>
        new ConsoleEventHandler(consoleGame);

    public void Dispose() =>
        Interlocked.Exchange(ref _subscription, null)?.Dispose();

    private void HandleNotification(GameNotification notification)
    {
        switch (notification)
        {
            case LogAddedNotification:
                _consoleGame.LogPrinter.PrintNewestLogs(10, _consoleGame.CurrentGame.Logs.LogList);
                break;

            case CardDrawnNotification cardDrawn:
                _consoleGame.CardPrinter.PrepareAndPrintSquareCard(
                    _consoleGame.CurrentGame.CurrentPlayer.Position,
                    cardDrawn.Card,
                    cardDrawn.PresentationToken);
                break;

            case SpaceReachedNotification spaceReached:
                _consoleGame.CardPrinter.PrepareAndPrintSquareCard(spaceReached.Space.Position);
                break;

            case BoardChangedNotification:
                _consoleGame.Printer.PrintGameBoard(
                    _consoleGame.TablePieces,
                    _consoleGame.CurrentGame.Players);
                break;

            case PlayerInformationChangedNotification:
                _consoleGame.Printer.DisplayPlayersInformation(
                    _consoleGame.CurrentGame.CurrentPlayer,
                    _consoleGame.CurrentGame.Players);
                break;
        }
    }
}
