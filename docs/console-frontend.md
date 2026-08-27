# Console frontend

## Purpose

This document defines the responsibilities and intended behavior of
`Monopoly.Console`.

The Console is the current reference frontend and a playable proof that
`Monopoly.Core` can run a complete Monopoly match. It is not the game engine
and must not become the source of game rules.

The target contract in this document is normative. The later current
implementation section is informational and records transitional behavior that
has not yet reached the target design.

## Console as the reference frontend

`Monopoly.Core` is the game. The Console presents that game through text,
keyboard input and terminal rendering.

The Console demonstrates one way to:

- Compose a Core match and supporting services.
- Collect setup information and player decisions.
- Invoke Core commands.
- Render authoritative match state and results.
- Save and load through a persistence implementation.
- Present completion and errors.

Future web, desktop, mobile or game-engine frontends may use different
interaction and rendering models. They must follow the same Core boundary, but
they do not need to copy Console classes, menus, synchronous input or terminal
layout.

## Responsibility boundary

### Console owns

- Menus and frontend navigation.
- Keyboard input and confirmation prompts.
- Terminal cursor positioning and colors.
- Board, card, log and player rendering.
- Frontend token names, symbols and visual colors.
- Presenting the valid actions exposed by Core.
- Translating player input into Core decisions or commands.
- Displaying Core results, notifications and errors.
- Selecting a configured save-store implementation.

### Console does not own

- Turn progression.
- Movement or passing GO.
- Dice and doubles rules.
- Square or card effects.
- Purchase eligibility.
- Auctions or rent claims.
- Payment and insufficient-funds rules.
- Jail behavior.
- Mortgage, building or trade legality.
- Bankruptcy settlement.
- Winner selection.
- Save-state validation or game reconstruction.

Console menus may hide unavailable actions as a usability feature, but Core must
still validate every submitted command. A frontend availability check is never
the rule-enforcement boundary.

### Frontend logic rule

Console may contain the logic required to operate a Console frontend. This
includes framework and terminal integration, navigation, input collection,
presentation state, formatting, rendering and translating a user action into a
Core command.

It must not contain logic that determines Monopoly behavior.

The boundary is:

> A frontend decides how a choice is presented and collected. Core decides
> which choices are allowed and what each accepted choice does to the match.

For example, Console may prevent letters from being entered in a numeric field
or hide a command that Core reports as unavailable. Core must still validate
the submitted number or command. A frontend convenience check cannot be the
only protection for a game invariant.

This rule applies equally to future web, desktop, mobile and game-engine
frontends. Their framework-specific code remains outside Core, while all
movement, economy, ownership, card, turn and winner behavior remains inside
Core.

## Dependency direction

The target composition is:

```text
Monopoly.Console -> Monopoly.Core
Monopoly.Console -> persistence abstraction or Infrastructure
Infrastructure implementation -> Monopoly.Core persistence contracts
```

`Monopoly.Core` must not reference Console classes or `ConsoleColor`.

Console-specific models may reference or project Core state for rendering. They
must not be passed back into Core as authoritative domain state.

## Composition root

`Program` is the Console composition root. It should create and connect:

- The Console I/O abstraction.
- Menu and rendering services.
- The selected and validated rule profile.
- Core game setup.
- The Console decision adapter.
- The configured persistence implementation.
- The Console game session.
- Match-scoped notification subscriptions.

Object construction for new and loaded games should share one composition path.
The difference is how the Core `Game` is obtained, not how every frontend
service is built.

The composition root may depend on concrete implementations. Domain and
rendering classes should receive their required collaborators rather than
constructing menus, Console wrappers or storage services internally.

## Main navigation

The target main menu provides:

- Start a new game.
- Load an existing game.
- Return to an active game when supported.
- Exit the application.

Navigation should use one controlled application/session loop. Opening a
submenu, returning to the main menu or cancelling an operation must not start a
second game loop or grow the call stack through recursive menu calls.

Leaving a match must dispose or detach its frontend subscriptions. Starting or
loading another match must not leave the previous Console session reacting to
Core notifications.

## New-game flow

The Console collects presentation and setup choices but delegates validation
and game creation to Core.

The target flow is:

1. Select UK Classic, US Classic or Custom.
2. For Custom, collect only options supported by the rule-profile contract.
3. Ask Core to validate and resolve the effective profile.
4. Collect the supported number of player names.
5. Assign frontend token symbols and visual colors.
6. Ask Core setup to create players with stable IDs and profile-defined money.
7. Let Core determine the starting player according to the profile.
8. Compose one Console session around the resulting `Game`.
9. Render the initial state and enter the session loop.

The Console must not hardcode Classic player limits, starting money, dice,
currency, first-player selection or other profile behavior.

Frontend token choices are not Monopoly ownership colors. They are
presentation-only identifiers associated with player IDs.

## Load-game flow

The target load flow is:

1. Ask the storage implementation to list or locate saves.
2. Let the user select a save without exposing storage rules to Core.
3. Read and deserialize the declared save version.
4. Ask Core to validate and reconstruct a new `Game`.
5. Keep the previous active match unchanged if any step fails.
6. Compose fresh Console runtime services around the loaded game.
7. Restore or collect presentation-only preferences separately.
8. Render the loaded state and continue from its exact Core phase.

The Console displays clear not-found, unsupported-version, invalid-state and
storage errors. It must not repair the save, substitute rules or partially copy
loaded state into the active match.

The complete persistence contract is defined in
[save-format.md](save-format.md).

## Console session loop

`Game.PlayTurn()` is the current entry point for one dice-roll and action
cycle. A roll of doubles can retain the same player's turn, so the Console must
follow the returned state rather than assume that each call advances to another
player.

A synchronous Console session typically performs:

```text
Render current state
        |
Read the current Core phase and allowed actions
        |
Collect one player action
        |
Submit a Core command or call Game.PlayTurn()
        |
Read the result and updated Game state
        |
Render once
        |
Continue until Core reports game over
```

Core is authoritative for `CurrentPlayer`, extra rolls, pending decisions,
bankruptcy and `Winner`. The Console must not increment turns, rotate players
or infer completion itself.

When Core introduces explicit pending phases, the session loop should render
the pending choice and submit the matching response command. Console may answer
synchronously, while the same Core state remains usable by asynchronous
frontends.

## Player decisions

The current synchronous adapter is `ConsolePlayerDecisionProvider`. It
implements `IPlayerDecisionProvider` and currently handles:

- Confirming a property purchase.
- Confirming whether to leave Jail.
- Allowing asset management when funds are insufficient.

The complete rules require richer decisions, including:

- A specific Jail action and, when applicable, a specific held card.
- Auction bids or withdrawal.
- Rent claims.
- Trade offers and confirmations.
- Mortgage handling after an ownership transfer.
- Building-shortage auction choices.
- Valid responses during bankruptcy settlement.

Core defines the decision type, available options and validation. Console:

1. Renders the Core-provided options.
2. Collects one response.
3. Submits the response to Core.
4. Displays the accepted result or validation error.

Prompts must use values from the resolved match state, such as currency, price
and Jail fine. They must not contain hidden rule constants.

## Menus and transaction windows

The player action menu may expose commands such as:

- Roll or continue the current turn phase.
- Manage buildings.
- Mortgage or unmortgage property.
- Propose or respond to a trade.
- Save the match when Core reports that saving is safe.
- Return to application navigation.

The available menu is driven by the current Core phase and eligibility queries.
For example, Console can ask which properties may currently be mortgaged, but
the mortgage command must repeat authoritative validation inside Core.

Asset management used to resolve insufficient funds is a constrained
transaction window. Console must show only permitted actions and return control
to the outstanding payment flow. It must not create money, mark a debt paid or
declare bankruptcy itself.

Menu cancellation and back navigation must have explicit results. Returning
from a menu must not accidentally roll, repeat a payment, duplicate a
transaction or abandon mandatory state.

## Rendering

Rendering reads Core state without mutating it.

The Console presentation includes:

- Board positions and player tokens.
- Current player and balances.
- Square and title-deed information.
- Chance and Community Chest cards.
- Buildings, ownership and mortgages.
- Game logs and recent results.
- Prompts, menus, errors and winner information.

Console presentation models such as `TablePiece` and `SquareCard` may format
Core state for terminal output. They are disposable projections, not an
alternative domain model.

Property groups are represented by `PropertyGroup` in Core. Console maps each
group to a `ConsoleColor` locally. A web frontend may map the same group to CSS
or RGB without changing Core.

Color must not be the only way important information is communicated. Text,
symbols or labels should continue to identify players and property state when
terminal colors are unavailable.

All displayed rule values and regional names come from the resolved profile or
current Core state. Rendering must not duplicate board prices, rent tables,
salary, tax, Jail or card rules.

## Terminal interaction

The Console frontend may use cursor positioning, colors and arrow-key menus, but
it should handle unsupported or undersized terminals clearly.

Interactive controls must be consistent:

- Arrow keys move the current selection.
- Enter accepts the highlighted action.
- Escape cancels only where cancellation is valid.
- Confirmation choices state their effect before submission.

The cursor and colors should be restored when leaving the application or after
an unrecoverable frontend error.

`IConsoleWrapper` is the boundary for terminal I/O. Rendering and input
components should use that abstraction so they can be tested without a real
interactive terminal.

## Notifications and refresh

Core notifications may tell Console that logs, cards, board state or player
information changed. They are presentation hints only.

The Console must:

- Scope subscriptions to the active match.
- Ignore notifications from another match.
- Unsubscribe when the session ends.
- Avoid rendering the same change both from an event and an unconditional
  duplicate refresh.
- Derive displayed values from current state rather than treating an event as
  authoritative history.

The target Core notification boundary is match-scoped rather than static.
Console should be able to create more than one session over time without shared
mutable event state.

A single action may produce several Core changes. Console may coalesce them into
one final refresh while still presenting important intermediate cards or
decisions.

## Save and load integration

Console owns the user interaction for:

- Naming a save.
- Selecting its location where applicable.
- Confirming replacement.
- Listing and selecting existing saves.
- Displaying storage and validation errors.

Core owns save eligibility, state mapping, validation and reconstruction.
Infrastructure owns serialization and physical storage.

Saving must not recursively reopen the player menu or change turn state. Loading
must produce one newly composed session and must not retain subscriptions or
decision adapters from the previous match.

## Error handling

Recoverable frontend and storage errors should be displayed without terminating
the process or corrupting the active session. Examples include:

- Invalid setup input.
- Unsupported terminal behavior.
- Save not found.
- Malformed or unsupported save.
- Storage access failure.
- A Core command rejected because state changed or the action is no longer
  valid.

Domain failures returned by Core should be translated into clear player-facing
messages. Console must not catch a domain error and then apply a fallback rule
of its own.

Unexpected errors must still release subscriptions and restore terminal state
through session cleanup.

## Running manually

From the repository root:

```text
dotnet run --project Monopoly.Console
```

The project requires the SDK selected by `global.json`.

A manual smoke test should verify:

1. The main menu can start a new game.
2. Valid player and token choices create one match.
3. The board and current-player information render.
4. At least one roll moves and resolves through Core.
5. A Core decision prompt can be answered.
6. A match can be saved and loaded.
7. The loaded match can continue.
8. Returning or exiting removes the active session subscriptions.

Full gameplay-rule verification belongs in automated Core integration tests,
not only in this manual frontend check.

## Current implementation

The current Console is playable but remains a transitional reference frontend.

### Current composition

`Program.Main()` creates a `ConsoleWrapper`, menu selector and `MainMenu`.
`Program.StartNewGame()` and `Program.LoadGame()` each construct printers,
input, token selection, decision provider and `ConsoleGame` separately.

New-game setup currently asks only for player count, creates two six-sided dice
through `GameRules`, and uses the current Core setup. `CoreGameSetup` creates
players with the current `Player` default of 3,000 and selects the first created
player without an opening roll. It does not yet collect player names or offer
complete UK Classic, US Classic and Custom profile selection.

The current player-count menu offers two through eight players. This is current
UI behavior, not the documented Classic profile limit.

The composition root creates one `JsonFileGameSaveStore` for the fixed default
file `game_data.json` and passes it through the current menu/session chain.
Loading handles missing, invalid, incompatible-version and storage failures,
then constructs a new set of Console services after success. Presentation token
choices are collected again after load.

### Current game loop

`ConsoleGame.StartConsoleGame()`:

1. Sets fixed terminal positions.
2. Subscribes `ConsoleEventHandler`.
3. Renders the initial board.
4. Opens a `PlayerActionMenu` for `Game.CurrentPlayer`.
5. Calls `Game.PlayTurn()` when Roll is selected.
6. Refreshes the board, player information and landed-square card. Newest logs
   refresh only when Core publishes `LogAddedEvent`.
7. Continues until Core reports game over.
8. Displays `Game.Winner`.
9. Unsubscribes in a `finally` block.

There is one active Console game loop, but several menus currently navigate by
constructing another menu and calling its display method. Returning to main
navigation can therefore nest menu calls rather than transition through one
explicit application loop.

### Current decisions and asset menus

`ConsolePlayerDecisionProvider` currently returns Boolean answers. Its Jail
prompt contains a literal fine of 50 instead of always displaying the configured
Core value.

When funds are insufficient, it opens the real-estate menu and reports progress
only when the player's cash increases.

`HouseMenu` and `MortgageMenu` build availability lists from Core queries but
also perform some rule-like checks before calling public `Transaction` methods.
The target is for those menus to present eligibility while Core commands enforce
all legality.

### Current rendering and events

`ConsolePrinter`, `ConsoleCardPrinter` and `ConsoleLogPrinter` render through
`IConsoleWrapper` for most operations. `ConsoleGame` still calls
`System.Console.Clear()` directly.

`SquareCardBuilder` creates Console presentation cards from Core squares. It
currently stores rules and currency in static mutable properties and reads the
legacy `PropertySquare.Color` value from Core.

`ConsoleEventHandler` keeps one static current Console session and subscribes
to static Core events. It filters notifications by the sending `Game` and
unsubscribes the previous session before attaching another one. Repeated
subscription of the current session is idempotent, and cleanup from an already
replaced session does not detach the active session.

`LogAddedEvent` is the single rendering path for the newest-log view. The
post-turn refresh reads the other current match state but does not render logs
again, so one notified log change produces one log-view refresh.

### Current save behavior

`PlayerActionMenu` uses the injected `IGameSaveStore`. The file implementation
writes a same-directory temporary file and atomically promotes it, preserving
an existing save when writing or promotion fails. A typed save failure is shown
without terminating the process.

The menu still opens the player action menu again after saving. Save naming,
save selection and the recursive session/menu cleanup remain assigned to their
focused Console issues.

The exact current schema and limitations are documented in
[save-format.md](save-format.md).

## Testing responsibilities

Console tests should verify presentation and interaction behavior without
retesting Monopoly rules.

They should cover:

- Menu navigation, cancellation and explicit return results.
- Mapping Core-provided choices to submitted commands.
- Decision prompts using profile values.
- Board, card, ownership, mortgage and building rendering.
- `PropertyGroup` to `ConsoleColor` mapping.
- Log refresh without duplicate presentation.
- Match-scoped subscription and cleanup.
- New-game and load-game composition through the same path.
- Save selection and error presentation.
- Winner display and clean session exit.
- Terminal operations through `IConsoleWrapper`.

Core integration tests remain responsible for movement, payments, Jail, cards,
auctions, trades, bankruptcy and winner rules. Infrastructure tests remain
responsible for serialization and storage correctness.

Detailed test strategy belongs in [testing.md](testing.md).

## Related documentation

- [architecture.md](architecture.md) defines the Core/frontend boundary.
- [game-flow.md](game-flow.md) defines turn phases and decisions.
- [game-rules.md](game-rules.md) defines rule profiles and game behavior.
- [save-format.md](save-format.md) defines persistence and reconstruction.
- [development-workflow.md](development-workflow.md) defines delivery and
  verification.
