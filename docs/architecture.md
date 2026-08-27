# Architecture

## Purpose

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

`Game.PlayTurn()` is the central entry point for one dice-roll and action cycle. A player may retain the turn after rolling doubles, so one call does not always represent the player's entire turn.

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
12. Returning a `TurnResult` that tells the frontend what happened and whether the player receives another roll.

No frontend should implement a second version of these rules. All frontends must use `Monopoly.Core` as the single source of truth for the game.

A synchronous frontend such as the current Console can interact with Core like
this:

```text
Create or load Game
        |
Provide IPlayerDecisionProvider
        |
Call Game.PlayTurn()
        |
Read TurnResult and current Game state
        |
Render the result and request the next user action
```

Frontends may read exposed game state for presentation. State changes go through
`Game.PlayTurn()` or validated `Game` commands. The current explicit asset
commands are `TryBuyHouse()`, `TrySellHouse()`, `TryMortgageProperty()` and
`TryRepayMortgage()`. An expected rule rejection returns `false` before any
mutation; null or foreign aggregate objects cause an argument exception.

`SetDecisionProvider()` reconnects the frontend's runtime decision service. The
provider is intentionally not persisted and is not an authoritative state
setter.

A web frontend cannot be assumed to answer a decision during the same method
call. Core must be able to represent a pending decision in match state, return
control to the caller and continue through a later command. The decision
boundary describes game choices, not Console input or a particular transport
such as HTTP.

## Core responsibilities

### Game

`Game` orchestrates the game and exposes the main public API: `PlayTurn()`, the
validated asset commands and read-only state queries. Movement, player rotation,
bankruptcy removal and payment primitives are internal Core operations.

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

- `IPlayerDecisionProvider` supplies decisions that require user interaction.
- `IDie` allows dice behavior to be provided and controlled in tests.
- `IGameLog` exposes read-only game log entries without coupling Core to a UI;
  log creation remains internal to the aggregate.
- `TurnResult` describes the result of a call to `Game.PlayTurn()`.

These abstractions are the current integration boundary. A frontend should not
depend on Core internals. Tests that need a prepared live aggregate use the
internal `GameTestBuilder`, which arranges detached Version 1 DTO state and then
passes through the same validated reconstruction boundary as loading. The
boundary will grow with rule-driven decisions while keeping those decisions
independent of any particular UI technology.

## Frontend decisions

Some actions require a player decision. These decisions are supplied through `IPlayerDecisionProvider`.

The current Core requests decisions for:

- Buying a property
- Paying to leave jail
- Resolving insufficient funds

The Console implements this interface with `ConsolePlayerDecisionProvider`.

A future web or game frontend can provide its own implementation without changing the Core rules.

The completed rules require additional decisions, including auction bids, rent
claims, jail actions, trades, mortgage handling after ownership transfer and
building-shortage auctions. These are Core-defined choices: the frontend
collects an answer, while Core validates and applies it.

Decision APIs may be immediate for local frontends or represented as pending
match state for asynchronous frontends. A frontend response must never bypass
Core validation.

## Events

Core events are notifications for presentation and integration.

They may notify a frontend when:

- A log is created
- A square is reached
- A card is drawn
- The board changes
- Player information changes

Events must not be the source of truth for game state or turn progression.

The Console subscribes to events through `ConsoleEventHandler` and removes its subscriptions when a console game ends.

The current Core events are static. This requires careful subscription cleanup
and is not a suitable long-term boundary for multiple concurrent matches, such
as games hosted by a web server. The target is notification state isolated per
`Game` instance. An application or infrastructure implementation may forward
those notifications, but matches must not share event state.

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

Save files store IDs instead of duplicated object references. During load:

1. Rules are reconstructed.
2. Players are recreated.
3. The board and card decks are built.
4. Owners and current player references are restored by ID.
5. Jail, fines, turn state and deck order are restored.
6. The resulting game is validated.

When rule profiles and interruptible interactions are implemented, a save must
also preserve the resolved rule profile and any valid pending match state, such
as an auction, rent claim, trade or debt settlement. A loaded game must continue
under the exact same effective rules.

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

`ConsoleGame` owns the Console game loop, but it delegates all game rules to `Game.PlayTurn()`.

Console-only models include `TablePiece`, `SquareCard` and the different printer and menu classes.

`Program` is the Console composition root. It creates the Core game, Console services and decision provider, then connects them. Each future frontend should have its own equivalent composition root outside Core.

## Colors and frontend presentation

Property color groups are part of the Monopoly domain and are represented in Core by `PropertyGroup`.

Visual color values are frontend-specific:

```text
PropertyGroup.Red
    Console frontend: ConsoleColor.DarkRed
    Web frontend: CSS or RGB color
    Game frontend: material or sprite color
```

`ConsoleColor` should not be part of the reusable Core model. Removing the remaining Core dependency on `ConsoleColor` is tracked in issue [#32](https://github.com/Extender92/Monopoly/issues/32).

After that refactor, this document should describe only the permanent
`PropertyGroup` boundary and should no longer need the temporary issue
reference.

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
