# Architecture

## Purpose

> **Target-boundary notice:** The positive publication target is now defined in
> [Public engine scope](public-engine-scope.md). Product-shaped types, profiles
> and rule lists below describe the current legacy implementation or migration
> context unless they are compatible with that scope. Issues #37 and #72–#77
> own the remaining neutral contracts and documentation updates.

Monopoly is split into a reusable game Core, a console frontend, a persistence
infrastructure implementation and automated tests.

The long-term goal is to allow different frontends, such as a web application, desktop game or mobile game, to use the same Monopoly.Core game logic without duplicating rules.

Unless a section explicitly describes the current implementation, this
document defines the target architecture. Current compatibility details do not
override the permanent dependency and responsibility boundaries.

## Project structure

The repository currently contains four projects:

```text
Monopoly.Core
    Reusable game rules, state and services

Monopoly.Console
    Console menus, keyboard input and rendering

Infrastructure
    JSON serialization and atomic file persistence

Monopoly.Tests
    Core, Infrastructure and Console tests
```

Project dependencies:

```text
Monopoly.Console -> Monopoly.Core
Monopoly.Console -> Infrastructure
Infrastructure -> Monopoly.Core persistence contracts
Monopoly.Tests -> Monopoly.Core
Monopoly.Tests -> Monopoly.Console
Monopoly.Tests -> Infrastructure
```

`Monopoly.Core` does not reference the Console project.

The Infrastructure project is currently a neutral placeholder whose final
identity is selected by #56. Its dependency direction remains:

```text
Frontend/Application -> Monopoly.Core
Frontend/Application -> Infrastructure implementation
Infrastructure implementation -> Core persistence contracts
```

Core must not depend on a frontend, a database, a file system or another concrete
storage technology.

## Monopoly.Core

`Monopoly.Core` contains the domain model and is responsible for the complete game state and game rules.

## Core as the game

`Monopoly.Core` is the game. It contains the authoritative game state, rules and game flow.

A frontend is only a way to interact with and present the game. It may provide input, rendering and frontend-specific navigation, but it must not reimplement turn progression, movement, payments, jail, cards, bankruptcy or winning rules.

`Monopoly.Console` is the current reference frontend and a playable proof that the Core can run a complete game. It is not the source of the game rules.

Future web, desktop, mobile or game-engine frontends must use `Monopoly.Core` in the same way and must not create their own rule implementations.

One `Game` instance represents one match and owns that match's state.

The main aggregate is `Game`. It owns:

- Players and the current player
- Game rules
- The board and squares
- Dice
- Jail state
- Transactions
- Fortune card decks
- Fines
- Turn and doubles state
- Current phase, a pending decision and primitive continuation data
- Bankruptcy and winner state

As the rules engine is completed, the match state must also own any active
auction, rent claim, trade, debt settlement, building inventory and pending
player decision. This state belongs to the match even when a frontend displays
or collects the decision.

A game is normally created through `CoreGameSetup.Setup()`.

`Game` is the public mutation boundary for the live aggregate. Frontends receive
getter-only state, `IReadOnlyList`/`IReadOnlyDictionary` collections backed by
non-castable read-only wrappers, and immutable player, square, Jail and rule
properties. They cannot rotate players, move tokens, execute square or card
effects, change balances or ownership, draw cards, edit Jail entries or append
game logs directly.

## Turn flow

`Game.PlayTurn()` is the central entry point that starts one dice-roll and
action cycle. The call runs until the cycle completes, the game ends or Core
needs a frontend answer. `Game.SubmitDecision()` resumes the same cycle from
that stored boundary. A player may retain the turn after rolling doubles, so a
completed cycle does not always represent the player's entire turn.

The Core is responsible for:

1. Checking whether the game is over.
2. Handling a player who is bankrupt.
3. Handling jail turns.
4. Rolling the dice.
5. Counting doubles.
6. Moving the player and wrapping around the board.
7. Paying salary when passing GO.
8. Resolving the landed square.
9. Handling purchases, rent, taxes and cards.
10. Handling bankruptcy.
11. Selecting the next active player.
12. Returning a `GameActionResult` that either contains the completed
    `TurnResult`, exposes the immutable pending decision or describes a typed
    rejection.

No frontend should implement a second version of these rules. All frontends must use `Monopoly.Core` as the single source of truth for the game.

A synchronous frontend such as the current Console can interact with Core like
this:

```text
Create or load Game
        |
Attach the insufficient-funds runtime provider
        |
Call Game.PlayTurn()
        |
If DecisionRequired, render PendingDecision
        |
Call Game.SubmitDecision(DecisionResponse)
        |
Read the completed TurnResult and current Game state
        |
Render the result and request the next user action
```

Frontends may read exposed game state for presentation. State changes go through
`Game.PlayTurn()`, `Game.SubmitDecision()` or validated `Game` commands. The current explicit asset
commands are `TryBuyHouse()`, `TrySellHouse()`, `TryMortgageProperty()` and
`TryRepayMortgage()`. An expected rule rejection returns `false` before any
mutation; null or foreign aggregate objects cause an argument exception.

`SetDecisionProvider()` reconnects the temporary insufficient-funds runtime
service. The provider is intentionally not persisted and is not an
authoritative state setter. Property-purchase and Jail choices are immutable
Core state and never invoke frontend input while a Core call is active.

A web frontend is not assumed to answer a decision during the same method call.
`Game.Phase` distinguishes `ReadyForTurn`, `AwaitingDecision` and `GameOver`;
`Game.PendingDecision` carries a stable `Guid`, participant ID and read-only
allowed responses. Core validates a later `DecisionResponse` before mutation
and retains primitive continuation data so dice, movement and rotation are not
repeated. The boundary describes game choices, not Console input or a
particular transport such as HTTP.

## Core responsibilities

### Game

`Game` orchestrates the game and exposes the main public API: `PlayTurn()`,
`SubmitDecision()`, the validated asset commands and read-only state queries.
Movement, player rotation, bankruptcy removal and payment primitives are
internal Core operations.

### GameHandler

The internal `GameHandler` contains shared game operations such as:

- Dice rolling
- Movement and board wrapping
- Salary when passing GO
- Doubles detection
- Asset calculation
- Payment resolution
- Bankruptcy handling

### Transaction

The internal `Transaction` type performs money and property transactions,
including:

- Buying properties
- Paying rent
- Paying taxes and fines
- Mortgage operations
- Buying and selling houses or hotels
- Receiving money from the bank

### Board and squares

`GameBoard` owns the 40 board squares.

`Square` is the base type for board locations. Specific square types implement
their internal landing behavior through `LandOn()`; a frontend cannot invoke a
landing effect independently of Core turn flow.

Examples include:

- `PropertySquare`
- `RailroadSquare`
- `UtilitySquare`
- `TaxSquare`
- `ChanceSquare`
- `CommunityChestSquare`
- `GoToJailSquare`
- `JailSquare`
- `ParkingSquare`

### Rules and data

Each match uses one resolved, validated rule profile. The supported profile
types are:

- UK Classic
- US Classic
- Custom

UK Classic and US Classic are fixed presets based on the selected official
editions. Custom starts from a defined baseline and may override supported
options. A profile includes both behavioral rules and the matching board, card
and economic data needed to create a coherent game.

The resolved profile should be immutable for the duration of a match. Code must
not branch on frontend, display language or currency symbol to decide game
behavior.

The current `GameRules` class and UK/US data selection are an earlier,
partially configurable implementation of this model. They will be replaced or
adapted as the profile architecture is implemented. Detailed profile behavior
belongs in [game-rules.md](game-rules.md) and the documents under
[`rules/`](rules/).

## Core integration points

The public Core API provides explicit integration points for frontends and tests:

- `IPlayerDecisionProvider` supplies only the transitional synchronous
  insufficient-funds callback.
- `IDie` allows dice behavior to be provided and controlled in tests.
- `IGameLog` exposes read-only game log entries without coupling Core to a UI;
  log creation remains internal to the aggregate.
- `IGameNotificationSource` exposes one match-scoped stream of
  non-authoritative presentation hints. `Subscribe()` returns an idempotent
  `IDisposable` lifetime handle; callers cannot publish through the interface.
- `GameActionResult` describes completion, a required decision, game over or a
  typed rejection. A completed result contains its `TurnResult`.
- `PendingDecision` and `DecisionResponse` form the frontend-neutral resumable
  choice boundary.

These abstractions are the current integration boundary. A frontend should not
depend on Core internals. Tests that need a prepared live aggregate use the
internal `GameTestBuilder`, which arranges detached Version 1 DTO state and then
passes through the same validated reconstruction boundary as loading. The
boundary will grow with rule-driven decisions while keeping those decisions
independent of any particular UI technology.

## Frontend decisions

Some actions require a player decision. The current Core exposes property
purchase and Jail release as authoritative pending state:

- `PropertyPurchaseDecision` offers `Purchase` or `Decline`.
- `JailReleaseDecision` offers `LeaveJail` or `RollForDoubles` and carries the
  configured fine and current card/Jail context.

The Console renders these snapshots and submits `DecisionResponse`. The
remaining `IPlayerDecisionProvider.ResolveInsufficientFunds()` callback is
temporary transition technology for synchronous asset management while a
mandatory payment or an accepted purchase lacks cash.

A future web or game frontend can provide its own implementation without changing the Core rules.

The completed rules require additional decisions, including auction bids, rent
claims, jail actions, trades, mortgage handling after ownership transfer and
building-shortage auctions. These are Core-defined choices: the frontend
collects an answer, while Core validates and applies it.

Local frontends may answer immediately after Core returns, while asynchronous
frontends may retain the same match in `AwaitingDecision`. A frontend response
must never bypass Core validation.

## Notifications

Each `Game` owns one notification source for presentation and integration.

They may notify a frontend when:

- A log is created
- A square is reached
- A card is drawn
- The board changes
- Player information changes

Notifications are never the source of truth for state, decisions or turn
progression. Only Core can publish them. A subscriber failure is isolated from
rule execution and from other subscribers, and presentation code reads current
state after receiving a hint. Public authoritative operations reject reentrant
calls made while a notification callback is running.

`Game.Notifications.Subscribe()` returns an idempotent disposal handle. The
Console owns that handle for exactly one running session and disposes it on
every exit path. A completed match releases its subscriber references as a
final safety boundary. Two simultaneous matches own different publishers and
cannot deliver notifications to one another's subscribers.

An application or Infrastructure adapter may forward a match's notifications,
but there is no process-global Core event bus and notification callbacks are
not rule callbacks. The temporary insufficient-funds rule callback remains the
explicit `IPlayerDecisionProvider` contract rather than a presentation event.

The current Console project also has access to selected Core internals through `InternalsVisibleTo`. This is a compatibility detail of the current implementation, not an API model for future frontends.

## Save and load

The save schema, game-state validation and reconstruction belong to the Core because they must follow the domain model and work consistently for every frontend.

Core owns:

- The versioned save-state contract
- Mapping a match to save state
- Validation of persisted domain state
- Reconstructing a valid match

Infrastructure owns:

- File access
- Databases
- Browser storage
- Cloud storage
- Serialization transport details that are not domain rules

The frontend or application composition root selects the storage
implementation through `IGameSaveStore`. Core's `GameStateV1Mapper` owns the
Version 1 state mapping, validation and reconstruction. Infrastructure's
`JsonFileGameSaveStore` owns JSON, paths, technical error translation and
atomic file replacement.

Version 1 cannot represent `AwaitingDecision`. Mapping such a game is rejected
before serialization or storage access, so an existing destination is not
replaced. Core also exposes detached primitive-only progress, decision and
continuation DTO projections for future Version 2 work; those DTOs are not
part of the Version 1 envelope.

Save files store IDs instead of duplicated object references. During load:

1. Rules are reconstructed.
2. Players are recreated.
3. The board and card decks are built.
4. Owners and current player references are restored by ID.
5. Jail, fines, turn state and deck order are restored.
6. The resulting game is validated.

Version 2 must preserve the resolved rule profile, the current purchase/Jail
decision progress and future pending match state such as an auction, rent claim,
trade or debt settlement. A loaded game must continue under the exact same
effective rules.

Presentation-specific values should not be stored in save files.

Detailed format and compatibility rules belong in [save-format.md](save-format.md).

## Console frontend

`Monopoly.Console` is responsible for:

- Starting and loading games
- Collecting player input
- Rendering the board and cards
- Displaying logs and player information
- Providing Console-specific player decisions
- Mapping Core values to Console values

`ConsoleGame` owns the Console game loop, but it delegates all game rules to
`Game.PlayTurn()` and `Game.SubmitDecision()`. It synchronously renders and
answers pending decisions until the current Core action completes.

Console-only models include `TablePiece`, `SquareCard` and the different printer and menu classes.

`Program` is the Console composition root. It creates the Core game, Console services and decision provider, then connects them. Each future frontend should have its own equivalent composition root outside Core.

## Authoritative identity and profile presentation

Authoritative identity is separate from rendering. A property group is
identified by `GroupId`; ownership and fee rules compare that ID and never a
color, label or layout hint. Spaces, current decks and cards, statuses,
resources, pending decisions and notifications expose stable semantic
`PresentationToken` references.

Each `Game` owns exactly one immutable `ProfilePresentation` catalog. A catalog
entry may provide display text, short text, description, symbol, semantic color
and layout tokens. Missing display text falls back to short text and then the
token value. Missing optional description or symbol is omitted. A missing
referenced catalog entry is invalid configuration and prevents the match from
being returned.

Tokens contain at most 128 characters and use lowercase ASCII segments joined
by a period or hyphen. Catalog entries are unique and stored in ordinal order.
The current legacy composition builds this catalog internally; issue #74 will
embed the same contract in `ValidatedGameProfile` and include its canonical
content in the profile fingerprint. No fingerprint is calculated by the
presentation contract itself.

Visual values remain frontend-specific:

```text
semantic color token
    Console frontend: ConsoleColor
    Web frontend: CSS or RGB
    Game frontend: material or sprite
```

Core exposes no terminal color, CSS, brush, image or other frontend-framework
type. Text logs remain non-authoritative runtime messages and resolve profile
names and the primary resource symbol through the match catalog.

## Testing architecture

`Monopoly.Tests` contains:

- Core unit tests
- Core game-flow integration tests
- Core state-mapping and Infrastructure file-storage tests
- Console menu and printer tests
- Event subscription tests

Changes to game rules or state transitions should normally include integration tests.

Detailed test responsibilities and commands belong in [testing.md](testing.md).

## Documentation boundaries

This document describes project boundaries, ownership and dependency direction. Detailed behavior belongs in the focused documents:

- [game-flow.md](game-flow.md) for turn and match flow.
- [game-rules.md](game-rules.md) for Monopoly rules.
- [console-frontend.md](console-frontend.md) for Console interaction and rendering.
- [save-format.md](save-format.md) for persistence state and compatibility.
- [testing.md](testing.md) for the test strategy.
- [development-workflow.md](development-workflow.md) for repository workflow.

## Architectural principles

- Keep game rules in `Monopoly.Core`.
- Keep input and rendering in frontend projects.
- Use frontend-neutral decision contracts for player choices.
- Support pending decisions without assuming synchronous user input.
- Use events only for notifications.
- Isolate notifications and mutable state per `Game` instance.
- Use IDs when serializing references.
- Keep turn progression centralized in `Game.PlayTurn()`.
- Apply all state changes through validated Core operations.
- Resolve and validate one rule profile for the lifetime of a match.
- Avoid Console-specific types in Core.
- Do not duplicate Core rules in a frontend.
- Treat one `Game` instance as one isolated match.
- Keep storage technology outside the game rules.
- Compose frontend, Core and infrastructure dependencies at the application boundary.
