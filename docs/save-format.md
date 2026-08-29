# Save and load

## Purpose

This document defines how a Monopoly match must be persisted and reconstructed.
Its primary purpose is to describe the target contract for a complete save/load
implementation that works for the Console and future web, desktop, mobile or
game-engine frontends.

The target contract is normative. The later Version 1 section is informational
and describes the current transitional implementation and its limitations.

A successful load must produce the same playable match under the same effective
rules. Save/load must never depend on Console input, rendering or another
frontend-specific type.

## Responsibility boundaries

### Core

`Monopoly.Core` owns:

- The versioned game-state contract.
- The complete list of authoritative match state that must be persisted.
- Mapping a `Game` to persistable state.
- Validating persisted domain state.
- Reconstructing a complete and valid `Game`.
- Declaring whether the current match state is safe to save.
- Stable persistence abstractions required by callers.

Core must not read from or write to the Console. In the target architecture it
must also not select file names, directories, databases or cloud providers.

### Infrastructure

An infrastructure implementation owns:

- JSON or another storage representation.
- File, database, browser or cloud access.
- Listing, naming, selecting and deleting stored saves.
- Safe and atomic writes.
- Technical errors such as inaccessible storage or invalid serialized data.

The current implementation follows this boundary: Core exposes
`IGameSaveStore` and owns Version 1 state mapping, while the neutral
`Infrastructure` project implements JSON file storage.

### Frontend or application boundary

The composition root selects a storage implementation and connects it to Core.
A frontend may ask the user where to save or which match to load, but it must
not reconstruct domain objects, repair invalid state or apply game rules.

Frontend presentation state is separate from authoritative match state. Token
art, colors, open menus, animations and window layout must not be required to
reconstruct the game.

The current Version 1 wire format contains no `ProfilePresentation` catalog,
display text, symbol, color token or layout token. Loading Version 1 reconstructs
the transitional legacy catalog from the saved `GameLanguage`; the exact JSON
shape remains unchanged. Rendered values are never authoritative persistence
references.

The closed profile definitions introduced by #73 are also absent from Version
1. `ProfileId`, revision, fingerprint, generic resources, statuses,
capabilities and effects become persistent only through Save Format Version 2.
Issue #74 owns JSON canonicalization and fingerprint calculation, while #75
owns execution; persistence does not define either contract again.

## Save consistency

A save represents one internally consistent match state.

Core operations that must be atomic, such as transferring money or applying a
completed trade, may not be persisted halfway through their mutation. A failed
save must leave the active in-memory match unchanged.

An explicit pending player decision is stable match state rather than a partial
mutation. If Core has returned control while waiting for an auction bid, rent
claim, jail choice, mortgage decision, trade confirmation or debt-settlement
choice, that pending state must either:

1. Be persisted completely and safely resumed after load; or
2. Make the match temporarily ineligible for saving through an explicit Core
   result.

The completed architecture should prefer persistable pending state so hosted
and asynchronous games can continue across process restarts. It must never
silently discard a pending obligation or repeat an already applied effect.

## Required persisted match state

The logical save contract must contain all state that can affect future game
behavior.

### Format identity

- Save schema version.
- Any required game or match identifier.
- Rule-profile identity and revision.

The schema version describes the persisted data structure. The rule-profile
revision identifies the exact rules and game data used by the match. They are
separate compatibility concerns.

### Effective rules

A save must contain enough information to restore the exact effective rule
profile selected when the match was created:

- UK Classic, US Classic or Custom profile identity.
- The selected official preset revision where applicable.
- Every supported Custom override.
- Player limits and starting configuration.
- Dice configuration.
- Economy, salary, tax, jail, mortgage and Free Parking rules.
- Auction, rent-claim, building, trade and bankruptcy behavior.
- Board and card-data revision.

Loading a save after application defaults change must not silently change the
rules of the existing match. A stable preset identifier and revision may be
used when that exact preset remains available. Custom matches must persist
their resolved effective settings rather than depend on current UI defaults.

### Players and turn state

- Stable and unique player IDs.
- Player order.
- Player names.
- Money.
- Board positions.
- Active or bankrupt state where bankruptcy history is retained.
- Current player ID.
- Current turn or cycle state.
- Consecutive doubles state.
- Jail state and turns spent in Jail.
- Winner and game-over state when it cannot be derived unambiguously.

References must use IDs. A save must not serialize duplicate nested copies of a
player as owner, current player, creditor or decision participant.

### Board, bank and assets

- Square ownership by player ID.
- Mortgage state.
- House and Hotel state.
- Bank House and Hotel inventory.
- Free Parking or bank fine-pot state when enabled by the profile.
- Any other profile-defined finite bank resource.

Permanent board data such as names, prices and rents should normally come from
the resolved rule profile rather than be duplicated for every square. Every
persisted square must have a stable identity that is valid for that profile.

### Cards

- Stable card identity.
- Source deck for every card.
- Exact order for every `DeckId` present in the validated profile.
- Cards currently held by each player.
- Cards temporarily involved in a pending action, if any.

The same physical card identity may exist in exactly one location. A Get Out of
Jail Free card held by a player must not also remain in its deck. Loading must
restore the complete unique card set across decks and players.

### Pending match state

When present, pending state must include enough information to continue exactly
once. Depending on the implemented rules this can include:

- Current turn phase and permitted next commands.
- Unresolved property purchase or auction.
- Auction participants, active bidder, current bid and eligibility.
- Pending rent claim and its expiry boundary.
- Jail action and selected held card.
- Utility rent roll state.
- Trade offer, participants and confirmations.
- Mortgage handling after an ownership transfer.
- Payment obligations, creditor order and remaining debts.
- Bankruptcy settlement and remaining assets to process.
- Building-shortage auction state.

Pending state is owned and validated by Core. A frontend may display it and
submit a response but may not infer or recreate it independently.

## State that is not authoritative

The following values are not required as part of the domain save contract:

- Decision-provider instances.
- Event subscribers.
- Log handlers and other runtime services.
- Console menus and cursor positions.
- Frontend token colors, sprites, sounds or animations.
- Cached rendering models.
- Previous dice results after their effects are fully resolved.
- The runtime random-source instance, seed and consumption position.

Runtime services are injected again when the loaded match is composed.
Non-authoritative history or frontend preferences may be stored separately, but
game correctness must not depend on them.

Committed outcomes that still affect future behavior are authoritative. A
resumable continuation therefore carries its immutable dice purpose and
results. This is distinct from persisting or attempting to resume the runtime
random source itself.

## Reconstruction order

Loading must create a new match rather than partially mutate an existing game.
The target reconstruction order is:

1. Read the storage representation without changing an active match.
2. Identify and validate the save-schema version.
3. Deserialize into the DTO for that exact version.
4. Validate required sections, scalar ranges and unique identities.
5. Resolve and validate the exact rule profile and data revision.
6. Create players and index them by ID.
7. Build the board, bank inventory and canonical card sets from the profile.
8. Apply player and turn state.
9. Reconnect ownership, creditor and participant references by ID.
10. Apply buildings, mortgages, Jail and bank state.
11. Restore card locations and deck order by stable card identity.
12. Restore and validate any pending match state.
13. Recalculate only explicitly derived values such as a uniquely determined
    winner.
14. Run final whole-match invariant validation.
15. Return the new `Game` only after every step succeeds.

If any step fails, no partially loaded game is returned and the caller's current
match remains unchanged.

## Validation

Validation must reject structurally valid data that cannot represent a legal
Core state. At minimum it must verify:

- Supported and present schema version.
- Required sections and collection lengths.
- Valid rule-profile identity, revision and Custom values.
- Supported player count and unique player IDs.
- Existing IDs for current player, owners, creditors and decision participants.
- Valid board positions and square identities for the selected profile.
- Valid money, turn, doubles, Jail and fine values.
- Legal ownership, mortgage and building combinations.
- Building totals consistent with bank inventory.
- Complete, unique card identities across decks and players.
- Valid deck lengths and order for the selected profile.
- A valid turn phase and internally consistent pending decision.
- Bankruptcy, active-player and winner state consistency.

Validation must fail clearly. It must not silently skip unknown identities,
clamp invalid values, substitute current defaults or repair an ambiguous match.

Expected error categories include:

- Save not found.
- Unsupported or missing schema version.
- Malformed serialized data.
- Missing required state.
- Unknown profile or profile revision.
- Invalid reference.
- Invalid domain state.
- Storage unavailable or write failed.

Core and infrastructure must not pause for input when an error occurs. The
frontend decides how to present the failure and whether the user may try
another save.

## Versioning and compatibility

Every persisted save contains an explicit schema version.

- Compatible additions may retain the current version only when old data has an
  unambiguous, validated meaning and a documented default.
- Removing, renaming or reinterpreting persisted state requires a new version.
- Changing identity semantics, such as replacing jail-card counts with stable
  card identities, requires a new version unless a complete lossless migration
  exists.
- Readers must select the DTO and validation rules for the declared version.
- Unsupported and versionless files fail with a clear error.
- A newer application must not silently interpret an older save using the
  newest DTO.

Migration support is optional unless explicitly required by a release. When a
migration is provided it must be deterministic, tested and produce state that
passes the new whole-match validation.

## Storage safety

A storage implementation must not leave a partially written file or record as
the valid save if serialization or writing fails. File storage should write to a
temporary file in the target directory and replace the destination only after a
successful complete write.

The current file store writes through a unique same-directory temporary file,
flushes encoded content and the underlying file, then uses replacement for an
existing destination or a same-directory move for the first save. Failures
remove the owned temporary file where storage access permits and never delete
the previous destination.

Storage APIs should support explicit save names and locations, and should allow
the frontend to list and select available saves. Paths and naming rules are not
part of the Core game model.

## Current implementation: Version 1

Version 1 is the current transitional implementation. It does not yet satisfy
the complete target contract above.

`JsonFileGameSaveStore` uses `System.Text.Json` with indented output,
case-insensitive property matching, numeric enums and UTF-8 without a byte-order
mark. `GameStateV1Mapper` maps and reconstructs the logical state without file
or JSON dependencies.

Version 1 has no phase, pending-decision or continuation fields. A Version 1
load therefore reconstructs no pending interaction and always starts in
`ReadyForTurn`; an already completed match moves to `GameOver` when Core next
processes it. `GameStateV1Mapper.ToState()`
explicitly rejects `AwaitingDecision`; `JsonFileGameSaveStore.Save()` translates
that rejection before creating a temporary file or replacing an existing save.

`GameProgressState`, `PendingDecisionState` and `TurnContinuationState` are
detached DTO projections made from primitive values, enums and validated
profile/decision/space/resource/status IDs for future Version 2 work. They are not
serialized into the Version 1 envelope and have no physical persistence path
in this version. `TurnContinuationState` records the committed dice purpose,
individual results and derived roll values, but never a random-source instance, seed or
source position.

The current top-level JSON structure is:

```json
{
  "Version": 1,
  "Rules": {},
  "Players": [],
  "CurrentPlayerId": 0,
  "CurrentTurn": 1,
  "ConsecutiveDoubles": 0,
  "Fines": 0,
  "Squares": [],
  "Jail": [],
  "ChanceDeck": [],
  "CommunityChestDeck": []
}
```

Enums are currently serialized as their numeric values.

### Version 1 rules

`Rules` contains:

- `NumberOfPlayers`
- `NumberOfDice`
- `DieSides`
- `GameLanguage`
- `Salary`
- `DoubleOnGo`
- `FreeParking`
- `MortgageInterestRate`
- `JailFine`
- `MaxTurnsInJail`

This represents the current immutable `GameRules` model, not the target UK
Classic, US Classic and Custom profile model. Its complete constructor validates
the current numeric and enum ranges and supplies the existing default values.

### Version 1 players

Each item in `Players` contains:

- `Id`
- `Name`
- `Money`
- `Position`
- `NumberOfGetOutOfJailCards`
- `IsBankrupt`

`CurrentPlayerId` reconnects the current-player reference after all players are
created. Player list order is preserved.

Version 1 stores only a numeric jail-card count. It does not preserve a card's
stable identity or whether it came from Chance or Community Chest.

### Version 1 square state

`Squares` is sparse. A square is written only when it has an owner, is
mortgaged, or is a Property with one or more building levels.

Each written item contains:

- `Position`
- `OwnerId`
- `Houses`
- `IsMortgage`

The board is rebuilt from `GameRules`, after which this state is applied by
position. `Houses` values from zero through four represent Houses in the current
model; five represents one Hotel.

### Version 1 Jail and cards

Each `Jail` item contains `PlayerId` and `TurnsInJail`. Loading a Jail entry also
moves that player to the rebuilt board's Jail position.

`ChanceDeck` and `CommunityChestDeck` contain string representations of numeric
indexes into the current canonical card lists. These indexes preserve current
queue order but are not stable domain card identities.

Issue #52 replaces these transitional fields in Save Format Version 2. V2 uses
generic profile, space, deck and card IDs plus profile revision and canonical
fingerprint. Issue #74 defines how the presentation catalog contributes to that
fingerprint; this presentation refactor does not add fields to Version 1.

### Version 1 reconstruction

The current loader:

1. Requires `Version = 1`.
2. Validates the Version 1 DTO.
3. Recreates `GameRules` and players and receives a new runtime random source.
4. Constructs a new `Game`, which rebuilds its board, Jail and card handler
   without shuffling or consuming that source.
5. Restores fines, turn and doubles state.
6. Reconnects square owners by player ID.
7. Restores mortgage, buildings and Jail state.
8. Restores both card queues.
9. Derives a winner when exactly one active saved player remains.

Version 1 keeps its exact existing wire representation: it stores neither a
random source nor `LastDiceRoll`. Queue reconstruction uses the saved order and
does not perform a throwaway shuffle first. The Console composes a fresh
`SystemMatchRandomSource` for each loaded match.

The Version 1 DTOs remain mutable serialization data and are deliberately
separate from live match objects. Reconstruction creates new players, board
state, Jail state and card queues and applies data only through internal restore
operations. A DTO or source list retained by a caller cannot mutate the
reconstructed game afterward. Validation and reconstruction complete before the
new `Game` is returned, so failure cannot partially change an existing match.

`IPlayerDecisionProvider` is the transitional insufficient-funds runtime service
rather than saved state. Loading may supply it during reconstruction, and a
frontend may later reconnect it with `Game.SetDecisionProvider()`. Purchase and
status choices are authoritative pending state rather than provider callbacks.

`IGameSaveStore.Load()` returns a newly reconstructed `Game`. The removed
`SaveCoreData`, `LoadCoreData` and `GameStateSerializer` APIs are not part of
the persistence boundary; compatibility applies to the Version 1 file format.

### Version 1 validation

The current loader validates:

- Version 1 and presence of all required sections and collection items.
- At least one player and unique player IDs.
- An existing, active `CurrentPlayerId`.
- A non-empty current player roster no larger than the configured
  `NumberOfPlayers`; eliminated players may already have been removed.
- Positive player, dice and die-side counts and all current rule enum and
  numeric ranges.
- Positive current turn and non-negative fines.
- Consecutive doubles from zero through two.
- Non-negative player money and jail-card counts, plus bankrupt-player asset
  consistency.
- Player and square positions from zero through 39.
- Unique saved square positions.
- Existing, non-bankrupt player IDs for owners and active Jail entries.
- Ownership only on purchasable square types.
- Building levels from zero through five, an owner for mortgages and buildings,
  and no buildings on a mortgaged property.
- Unique Jail entries, Jail positions and turn counts within the configured
  maximum.
- Exact card-deck lengths, unique indexes and indexes valid for the selected
  current card lists.
- Winner consistency derived from the remaining active players.

Infrastructure translates expected failures to `SaveStoreException` with one
of four categories: `NotFound`, `InvalidData`, `IncompatibleVersion` or
`StorageFailure`. Raw JSON and file exceptions are retained as inner exceptions
for diagnostics and are not exposed as unhandled Console errors.

### Version 1 limitations

Version 1 is the sole temporary exception to the generic deck contract. Its
wire representation retains the named `ChanceDeck` and `CommunityChestDeck`
fields and ordinal string keys so existing files remain compatible. Core maps
those fields to internal legacy deck IDs; no active runtime/profile API exposes
or copies that two-deck shape. Issue #4 rejects Version 1 after legacy content
is removed, and issue #52 replaces it with generic profile, deck and card IDs.
The public Version 1 DTO property names, including its regional and detention
fields, are a wire-compatibility exception and are not reusable runtime or
profile contracts.

Version 1 does not currently preserve or fully validate:

- Stable rule-profile identity or revision.
- Resolved Custom rules.
- Stable board and card identities.
- Get Out of Jail Free card ownership by identity and source deck.
- Bank House and Hotel inventory.
- Auctions, rent claims, trades or pending decisions.
- Resumable phase, consumed decision IDs or continuation data; saving while a
  decision waits is rejected atomically.
- Pending payment or bankruptcy settlement.
- Winner as an explicit field.
- Runtime services, logs, events or frontend state.
- Every legal relationship between ownership, mortgages and buildings.

The current tests verify Core-only Version 1 round trips for UK and US games,
an existing Version 1 JSON fixture, the stable wire shape, all error categories,
atomic create/replacement failure behavior and preservation of an existing file
when an awaiting-decision save is rejected.

## Current Console behavior

The Console currently saves and loads `game_data.json` relative to the process
working directory through one injected `JsonFileGameSaveStore`. Saving
atomically replaces that path without a save-selection flow. Loading displays
distinct missing, invalid, incompatible-version and storage errors.

After loading, the Console creates new UI services and asks for frontend token
choices again. Those token choices are presentation state and are not part of
Version 1.

Save naming and location selection remain Console integration work, not Core
rules. Version 1 remains supported only through the extraction: #4 later makes
it incompatible, and no release may be produced until #52 closes the temporary
save/load gap with Version 2.

## Testing requirements

Every change to persistence must include tests at the correct boundary.

Core state tests should cover:

- Complete logical state round trips without requiring Console interaction.
- UK Classic, US Classic and representative Custom profiles.
- Reference reconstruction by stable IDs.
- Ownership, mortgages, buildings and bank inventory.
- Jail state and held card identity.
- Turn, doubles, winner and game-over state.
- Exact deck order and unique card locations.
- Every supported pending match phase.
- Rejection of invalid IDs, ranges, collection lengths and domain invariants.
- Clear rejection of unsupported versions and profile revisions.

Infrastructure tests should cover:

- Serialization and deserialization for each supported version.
- Missing, malformed, inaccessible and partially written storage.
- Atomic replacement of an existing save.
- Save listing, naming and selection behavior.

Frontend tests should verify that errors are presented without changing the
active match and that loaded runtime services are composed correctly.

Detailed repository test guidance belongs in [testing.md](testing.md).

## Related documentation

- [architecture.md](architecture.md) defines project ownership and dependency
  direction.
- [game-flow.md](game-flow.md) defines turn phases and pending decisions.
- [game-rules.md](game-rules.md) defines the rule-profile model.
- [console-frontend.md](console-frontend.md) defines Console interaction.
- [development-workflow.md](development-workflow.md) defines how persistence
  changes are delivered.
