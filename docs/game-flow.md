# Game flow

## Current WIP boundary

Core now has a production setup boundary. `GameSetup.Create` returns a complete
initial match from an explicitly supplied validated profile and ordered player
identities. Console still stops before a session and reports that capability
execution is being completed. Loading reports a typed compatibility failure.
No fallback match is created.

Focused tests may run the internal data-free executor through small synthetic
compositions. Those fixtures are not product data and are never selected by
Console.

## Setup flow

The implemented setup flow is:

1. An application explicitly supplies a `ValidatedGameProfile`; Core has no
   default profile.
2. Core verifies the requested player count against the profile.
3. Core verifies every capability, effect, status and policy against one
   trusted setup registry.
4. Core creates player resource balances and places players at the declared
   start SpaceId.
5. Core creates and shuffles each declared deck with the match-scoped random
   source.
6. Core applies the declared starting-player policy.
7. Core returns a match only after all references and runtime state validate.

`fixed-order` consumes no setup randomness. `random` makes one
`SetupStartingPlayer` request. `highest-roll` uses the full profile dice set;
only tied leaders reroll in seat order, and setup fails after 128 tied rounds.
Deck shuffle, random-player selection and setup dice each use independent
sequence indices.

The Lantern Vale Demo declares 2–5 players, 2d8, fixed player order, 120 Lumen,
zero Renown and a 12-Lumen pass-origin reward.

## Target turn and decision flow

Issue #75 will execute supported declarations. A frontend calls PlayTurn until
Core either completes the action cycle, reaches game over or returns an
immutable pending decision. The frontend later submits a DecisionResponse with
the same decision ID.

Core validates stale, duplicate, unavailable and participant-mismatched
responses without mutation. Runtime callbacks are never stored in match state.

Before #75, a profile-created match remains in `ReadyForTurn` and `PlayTurn`
returns `CapabilityExecutionUnavailable` without changing resources, position,
deck order, ownership, logs, dice, notifications, phase or decisions.

The Demo baseline requires:

1. roll profile-defined dice;
2. move around the ordered track;
3. apply the pass-origin reward when policy permits;
4. resolve the destination;
5. offer an unowned purchasable space;
6. leave it unowned when the player declines;
7. charge a fixed usage fee when another participant owns it;
8. draw and rotate a card from the referenced generic deck;
9. apply its ordered declarative effects;
10. complete after the declared round limit and compare the score resource.

Movement card effects can use a relative offset or absolute SpaceId. They
declare whether the destination resolves and whether crossing the origin
applies the profile reward. Backward Demo movement ignores that reward.

## Match-scoped services

Every match has its own notification hub and random source. Random requests
carry a purpose and sequence index. All dice values are validated before the
outcome, logs, notifications or phase change are committed.

Notifications describe completed state changes for presentation. They are not
commands and a frontend may always render directly from current Core state.

## Persistence

No current save format can represent the validated profile runtime. Save and
load therefore remain unavailable until #52. The future format must persist
profile identity, revision, fingerprint and all state required to resume the
current phase without replaying earlier effects.
