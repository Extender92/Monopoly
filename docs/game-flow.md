# Game flow

## Purpose

This document defines the intended match and turn flow exposed by `Monopoly.Core`.

Core is the authoritative source for turn progression, movement, landing effects, payments, jail, doubles, bankruptcy, player rotation and winner state. A frontend collects decisions and presents results, but it must not reproduce this flow.

Detailed Monopoly rules belong in [game-rules.md](game-rules.md). Current defects and implementation work are tracked in GitHub Issues rather than in this document.

## Rule profiles

Every match is created from one validated rule profile:

- **UK Classic** uses the selected official British Classic baseline, London board data, pounds and British cards and names.
- **US Classic** uses the selected official American Classic baseline, American board data, dollars and American cards and names.
- **Custom** starts from an explicit base profile and applies supported rule overrides.

The profiles share the same game-flow implementation. A profile supplies rules and data; it must not introduce a second `PlayTurn()` implementation or frontend-specific rule handling.

Official Classic profiles are immutable definitions during a match. A Custom profile is validated before the match starts and then stored with the match so save/load and every frontend use the same effective rules.

## Terminology

- A **match** is represented by one `Game` instance and continues until a winner is determined.
- A **player turn** starts when a player becomes `CurrentPlayer` and ends when Core advances to another active player.
- A **roll/action cycle** starts with `Game.PlayTurn()` and may continue through
  one or more `Game.SubmitDecision()` calls. It includes at most one main roll
  and all effects caused by that roll.
- An **extra roll** is another roll/action cycle for the same player, normally granted after eligible doubles.

One player turn may therefore require several completed cycles, and one cycle
may require several Core calls when a decision pauses it.

`CurrentTurn` identifies the roll/action cycle within the current player's retained turn. It starts at one, increases when that player receives an extra roll and resets when Core advances to another player.

## Match setup

The current transitional `CoreGameSetup` creates a match as follows:

1. Validate the current rules and player count.
2. Create the players and select the first player in list order.
3. Create one `Game` with a match-scoped random source.
4. Build the ordered track, validate every current draw-space deck reference
   and prepare the `DeckId`-ordered deck collection.
5. Return the complete match without consuming setup-player or setup-dice
   randomness.

Issue #40 replaces the fixed first-player behavior with profile-selected
`fixed`, `random` or `highest-roll` setup policies. `SetupStartingPlayer` and
`SetupDice` random purposes are reserved for that work and are not an interim
policy API.

The current legacy composition still supplies its transitional setup values.
Issue #40 replaces that path with setup values and starting-player policy from
one validated profile.

Frontend setup screens collect names and allowed profile choices. Core validates the choices, creates the match and determines the first player.

## Frontend interaction

Before a roll, a frontend may let players inspect the game, perform eligible transactions, save the match or leave the frontend. These actions do not replace or partially execute `PlayTurn()`.

Classic allows building, mortgages, auctions and deals while a player is in jail and does not restrict every transaction to the current player. A digital frontend exposes these actions through safe transaction windows between atomic Core operations. Core validates the acting player and complete transaction; a frontend menu must not be the only rule guard.

When the player chooses to roll, the frontend follows this flow:

```text
Read CurrentPlayer and current Game state
        |
Call Game.PlayTurn()
        |
Read GameActionResult
        |
If DecisionRequired, render PendingDecision
        |
Submit DecisionResponse and repeat as needed
        |
Receive completed TurnResult or GameOver
        |
Read the resulting Game state
        |
Render the result and offer the next valid action
```

The frontend must use `GameActionResult` together with the current `Game`
state. A completed action contains `TurnResult`; a paused action contains the
same immutable pending snapshot exposed by `Game.PendingDecision`. Events may
provide intermediate presentation notifications, but they do not control the
turn.

## Resumable action boundary

`Game.Phase` is `ReadyForTurn`, `AwaitingDecision` or `GameOver`. The current
public decision boundary uses stable `DecisionKindId` and `DecisionOptionId`
values:

- `PurchaseDecision` contains the player ID, `SpaceId`, a generic
  `ResourceAmount` and the `Accept`/`Decline` responses.
- Status-related choices are visible as the generic `PendingDecision` base
  contract. The current detention payload is internal transition state used by
  the legacy executor and Console until #75 provides generic execution.

Each decision has a stable `Guid` for its lifetime. `SubmitDecision()` validates
the complete response before mutation. Null, malformed, stale, duplicate and
defined-but-disallowed responses are typed rejections and leave both live state
and pending progress unchanged. Calling `PlayTurn()` while a decision waits is
also rejected without rerolling or moving.

Core stores continuation data as typed IDs, primitive values, enums, dice
results and turn flags rather than delegates or domain references. An accepted response may
continue to another pending decision, such as rolling doubles out of Jail and
then reaching an unowned Utility. Previous dice, movement, payments and player
rotation occur at most once.

The only remaining synchronous decision-provider operation is
`ResolveInsufficientFunds()`. It is temporary until debt and payment phases are
made resumable.

## Roll/action cycle

`Game.PlayTurn()` performs the following high-level sequence:

1. Check whether the match has already ended.
2. Identify the current active player.
3. Use the jail flow if that player is in jail.
4. Otherwise roll the configured dice.
5. Evaluate consecutive doubles before normal movement.
6. Move through the central movement operation.
7. Resolve the complete landing chain.
8. Resolve required payments, decisions and possible bankruptcy.
9. Decide whether the player receives an extra roll or the next active player takes over.
10. Determine the winner when only one active player remains.
11. Return `GameActionResult`: a completed `TurnResult`, a pending decision or
    game over.

If the match is already over, `PlayTurn()` performs no roll or state transition and returns a game-over result containing the winner.

## Normal roll flow

Every nondeterministic choice is requested from the source owned by that
`Game`. Ordinary movement uses `TurnDice`, detention attempts use
`DetentionDice` and an additional rule-specific roll uses
`DedicatedRuleDice`. Deck setup uses `DeckShuffle`. Purposes let tests and
diagnostics distinguish operations without making the source or its internal
state part of the match.

Core prepares all configured die values before it commits the roll. If any
request fails or returns an invalid value, no dice snapshot, log, notification,
phase, pending decision or rule state changes. A detention roll is prepared
before its pending response is consumed, so a failed source leaves the same
decision available for a later valid submission.

For a player who is not in jail:

1. Core rolls every configured die and records the results.
2. Core determines the total and whether all dice show the same value.
3. The consecutive-doubles count is updated as part of the same player turn.
4. On the third consecutive doubles, the player is sent directly to jail without moving by the rolled amount or resolving a normal landing.
5. Otherwise Core moves the player, handles board wrapping and resolves the reached square.
6. Core then checks whether the player became bankrupt or was sent to jail during landing resolution.
7. Eligible doubles grant an extra roll only when the player remains active and out of jail.
8. In every other case Core advances to the next active player.

Consecutive doubles reset whenever the current player's turn ends. Doubles from separate player turns must never be combined.

## Movement and board wrapping

All movement must use one Core-owned movement path. A frontend and individual square or card implementations must not reproduce wrapping or salary rules.

Forward movement that passes the end of the board wraps to the corresponding position. Core awards the configured salary once for each completed forward lap when the movement is eligible for salary.

Backward movement wraps without awarding salary. Cards that move to a named destination follow that card's salary rule while still using the shared movement operation.

Position zero is a valid landing position and must be resolved like every other square.

Movement notifications may be emitted for presentation after state changes, but movement does not depend on a subscriber.

## Landing chains

After movement, Core currently resolves the space through the internal legacy
`Square.LandOn()` executor. The public board exposes only an immutable
`SpaceView`; #75 replaces the executor with registered generic capabilities.

A landing may:

- Offer an unowned purchasable square.
- Charge rent owed to another player.
- Charge tax, a fine or another bank payment.
- Award money.
- Draw and execute a card from the space's referenced `DeckId`.
- Move the player to another square.
- Send the player to jail.
- Cause bankruptcy.
- Do nothing beyond completing the landing.

If a card causes movement, Core continues resolving the destination square. This creates one landing chain within the original roll/action cycle. Every movement in that chain must use the shared movement and wrapping rules.

`TurnResult.LandedSpace` represents the final resolved `SpaceView` in the
chain. It is `null` when the cycle has no landing, such as an internal status
transition that skips normal movement. Status changes are exposed separately
as immutable `StatusTransition` entries with player ID, `StatusId` and
apply/update/remove kind.

Intermediate card draws and reached squares may be presented through notifications without changing the final result or controlling the chain.

Deferred held-card capabilities must preserve the stable `CardId` and source
`DeckId` when a card temporarily leaves its deck. That capability is not part
of the first public baseline.

## Purchasing flow

When a player lands on an unowned purchasable square:

1. Core checks whether the square is eligible for purchase.
2. Core creates `PurchaseDecision` before money or ownership changes
   and returns control to the frontend.
3. If the player declines, no purchase-related asset management occurs and Core starts the configured auction flow.
4. If the player accepts but lacks cash, Core uses the insufficient-funds flow to let the player raise the required amount when their total eligible assets are sufficient.
5. Core purchases the square only after the decision is confirmed and the required cash is available.
6. If the confirmed purchase cannot be completed, the square remains unowned and Core starts the configured auction flow.

The frontend reports the decision; Core validates affordability and changes ownership and money.

The current #49 implementation deliberately leaves a declined or unsuccessfully
funded purchase unowned. The auction transition in the following target flow is
owned by #42 and is not yet part of the resumable purchase continuation.

### Auctions

UK Classic and US Classic require the bank to auction an unowned purchasable square whenever the landing player does not buy it.

Core owns the auction state and enforces the selected profile's bidding rules:

1. Every eligible active player, including the player who declined the printed-price purchase, may participate.
2. Frontends submit bids or pass through an injected decision boundary.
3. Core validates each bid against the current bid, available cash and profile rules.
4. The auction continues until no eligible participant challenges the highest valid bid.
5. The winner pays the bank and receives the square.
6. If no valid bid is made, the square remains unowned.

Custom profiles may change supported auction parameters but must explicitly choose whether auctions are enabled. A frontend must not implement its own auction state machine.

## Rent claim flow

Landing on an unmortgaged square owned by another player creates a potential rent claim.

In UK Classic and US Classic, the owner must claim the rent before the next dice roll. Core therefore records the pending claim, asks for the owning player's decision through the frontend boundary and resolves or expires it before another roll/action cycle begins.

- A valid claim uses the actual rent determined by the title and current state.
- A waived or expired claim transfers no money.
- Mortgaged properties collect no rent.
- A player never pays rent to themselves.
- Card instructions that modify railroad/station or utility rent are applied before the claim is resolved.

A Custom profile may enable automatic rent collection. Automatic rent is a declared custom rule, not a UK Classic or US Classic default.

## Payment and insufficient funds

Payments use their actual amount, regardless of whether the creditor is another player, the bank or the fines pool.

When one effect creates debts involving several players, Core creates and
settles the individual obligations in the deterministic active-player order
defined in [game-rules.md](game-rules.md). Each obligation retains its actual
debtor and creditor; the combined amount must never be treated as one bank debt.

If the player has at least the required cash, Core completes the payment immediately. Exact balance is sufficient.

If cash is insufficient:

1. Core checks whether the player can cover the amount with eligible assets.
2. If not, Core declares bankruptcy immediately.
3. Otherwise Core calls `IPlayerDecisionProvider.ResolveInsufficientFunds()` with the actual amount required.
4. The frontend may let the player perform valid Core operations such as selling buildings or mortgaging property.
5. Core verifies that the player's available cash actually increased before requesting another decision.
6. If no progress is made, Core declares bankruptcy instead of repeating indefinitely.
7. Once enough cash is available, Core performs the original payment exactly once.

The decision provider must not transfer the original debt or declare bankruptcy itself.

This bankruptcy path applies to mandatory debts. An optional purchase is not a debt: if a confirmed purchase cannot be funded, Core cancels that purchase and proceeds to the configured auction flow without bankrupting the landing player.

## Jail flow

At the start of a jailed player's roll/action cycle, Core controls every release and roll transition.

### Release before rolling

Core asks the player to choose among the release options currently allowed by the profile and player state. The decision must distinguish paying the configured jail fine, using a selected Get Out of Jail Free card and remaining in jail to attempt doubles.

When the player chooses and successfully completes a valid pre-roll release:

1. Core removes the jail state exactly once.
2. Core rolls the dice.
3. Core moves the player through the normal movement path.
4. Core resolves the complete landing chain.
5. Core applies the profile's doubles rule for a player who left jail before rolling. UK Classic and US Classic treat this as normal movement, so an eligible doubles roll grants another roll.

The frontend must display `GameRules.JailFine`; it must not contain a hard-coded jail amount.

### Attempting doubles

When the player remains in jail and rolls:

- Doubles release the player.
- Core moves by the rolled total and resolves the landing chain.
- Doubles used to leave jail do not grant an extra roll.
- A non-double increments the player's jail-turn count and normally ends the player turn without movement.

### Maximum jail turns

In UK Classic and US Classic, a Get Out of Jail Free card must be selected before rolling. If the third doubles attempt fails, Core requires the configured fine and then uses that third roll for movement through the same landing path. Custom profiles may alter this only through an explicit supported jail rule.

If the player cannot resolve the required fine, Core declares bankruptcy using the normal bank-creditor flow.

Jail membership and jail status must always change together. Looking up jail details for a player who is not jailed must fail clearly or return an explicit absence; it must never produce an unhandled dictionary error during normal play.

The current resumable boundary groups the pre-roll choice into `LeaveJail`
(use a held card first, otherwise pay the configured fine) and
`RollForDoubles`. The complete profile-driven release and third-roll movement
semantics remain owned by #30.

## Bankruptcy

Bankruptcy is resolved by Core as part of the action that created the unpaid debt.

For debt to another player:

- Buildings are sold according to the configured bankruptcy valuation.
- Remaining money and building-sale value are transferred to the creditor.
- Owned properties transfer to the creditor.
- Mortgage state follows transferred properties.
- Get Out of Jail Free cards transfer to the creditor.
- The creditor immediately resolves the profile's required mortgage interest or unmortgage choice for each mortgaged property received.

For debt to the bank:

- Owned properties return to the bank.
- Buildings are cleared.
- Mortgage state is cleared.
- Remaining player money is returned according to the bank-bankruptcy rule.
- Get Out of Jail Free cards return to the bottom of their originating decks.
- Returned properties are auctioned by the bank according to the profile.

In both cases, the bankrupt player is marked bankrupt and removed from active turn order. A bankruptcy during `PlayTurn()` must cause at most one player rotation; the next eligible player must not be skipped.

## Player rotation and winner

Core owns active-player rotation.

- An eligible extra roll keeps the same `CurrentPlayer`.
- A completed player turn selects the next non-bankrupt player in order.
- Bankrupt players are not selected again.
- Rotation wraps from the last player to the first active player.
- Changing players resets `CurrentTurn` and consecutive doubles.
- Removing the current player must still select the immediate next eligible player exactly once.

When only one active player remains, Core assigns that player to `Winner` and sets the match to game over. No frontend-specific loop is responsible for detecting or assigning the winner.

## Classic and custom spaces

UK Classic and US Classic use official Classic behavior for shared spaces. In particular, Free Parking has no reward or penalty.

Bonuses such as collecting fines on Free Parking, receiving double salary on GO or disabling auctions are Custom rules. They must be visible in the effective rule profile and persisted with the match. Regional names such as UK Super Tax and US Luxury Tax are board data rather than separate flow implementations.

## TurnResult

`PlayTurn()` and `SubmitDecision()` return `GameActionResult`. Its status is
`TurnCompleted`, `DecisionRequired`, `GameOver` or `Rejected`. Completion and
game-over results contain a `TurnResult` with the information required to
present the completed roll/action cycle:

| Field | Meaning |
| --- | --- |
| `Player` | The player whose cycle was processed. |
| `Roll` | Immutable canonical snapshot of the main roll, including purpose, individual results, sum and doubles state; `null` when no roll occurred. |
| `DiceResults` | The individual results rolled during the main cycle. Empty when no roll occurred. |
| `DiceSum` | The sum of the main roll, or zero when no roll occurred. |
| `LandedSpace` | Immutable generic view of the final space resolved by the landing chain, or `null` when no landing occurred. |
| `WasDouble` | Whether the main roll was doubles. |
| `StatusTransitions` | Immutable generic status changes applied, updated or removed during the cycle. |
| `ExtraTurn` | Whether the same player is entitled to another roll/action cycle. |
| `PlayerBankrupt` | Whether the processed player became bankrupt. |
| `GameOver` | Whether the match ended during or before this cycle. |
| `Winner` | The winning player when the match is over; otherwise `null`. |

The result is a summary rather than a second source of game state. The frontend should read current player balances, positions, ownership and other persistent state from the `Game` instance after the call.

`DiceResults`, `DiceSum` and `WasDouble` are derived from `Roll`. A card or
other rule may request a later dedicated roll and update `Game.LastDiceRoll`;
the completed result continues to describe the original movement roll.

## Notifications

Core notifications exist for rendering logs, movement, cards and changed player information. They may describe intermediate steps that are not all represented by the final `TurnResult`.

Notifications must be scoped to the correct match, must not execute game rules and must not be required for the flow to complete. A frontend may refresh entirely from `Game` and `TurnResult` without subscribing to them.

## Flow invariants

- One `Game` instance owns one isolated match.
- `PlayTurn()` starts a roll/action cycle and `SubmitDecision()` is the only
  public continuation entry point.
- One validated rule profile controls the complete match.
- UK, US and Custom matches use the same game-flow engine.
- Every movement and landing effect is resolved by Core.
- Every debt is paid once or ends in bankruptcy.
- Every declined or failed Classic purchase enters the Core auction flow.
- Every Classic rent claim is resolved or expired before the next roll.
- Insufficient-funds handling cannot loop without economic progress.
- A player is released from or added to jail exactly once per transition.
- A held Get Out of Jail Free card cannot remain available in its deck.
- A roll/action cycle advances to at most one next player.
- No frontend decides bankruptcy, player rotation or winner state.
