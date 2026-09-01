# Architecture

## Boundary

The publication target is a frontend-neutral property-trading engine, not a
universal board-game framework. Its positive boundary is defined in
[Public engine scope](public-engine-scope.md).

~~~text
Monopoly.Console --> Monopoly.Core
Monopoly.Console --> Infrastructure --> Monopoly.Core
Monopoly.Tests   --> all three projects
~~~

Core has no file-system, JSON, terminal or frontend dependency. Infrastructure
does not decide rules. Frontends do not mutate authoritative match state
directly.

## Profile model

Infrastructure parses untrusted schema-versioned JSON into
GameProfileDefinition. Core performs semantic validation and returns an
immutable ValidatedGameProfile.

A validated profile owns:

- profile identity, positive revision and canonical SHA-256 fingerprint;
- presentation metadata and resources;
- player and dice setup;
- an ordered track of arbitrary positive length;
- zero, one or many decks;
- generic space capabilities and declarative card effects;
- supported setup, pass-origin, declined-purchase and match-end policies.

ProfileId, SpaceId, DeckId, CardId, ResourceId and StatusId are separate
authoritative value types. PresentationToken selects optional display metadata
and never decides a rule.

The parser rejects unknown fields, unknown kinds, malformed encodings and
oversized input. Core rejects duplicate IDs, broken references, unsupported
combinations and incomplete presentation before a validated profile is
returned. Fingerprinting sorts unordered catalogs while preserving track,
card and effect order.

## Runtime boundary

Game is the authoritative match aggregate. `GameSetup.Create` is the public,
explicit construction boundary. It accepts one `ValidatedGameProfile`, ordered
player identities and an optional match-scoped random source. Core never
selects a default profile or reads a profile path.

Setup validates the trusted component registry before consuming randomness,
then prepares decks in ordinal DeckId order and applies the declared
starting-player policy. The same registry owns the closed execution handlers;
there is no second vocabulary or fallback engine.

`PlayTurn` prepares a transition against detached resource, position,
ownership and deck state. Core commits it only after every mutation is valid,
then publishes match-scoped notifications. Purchase decisions store a
primitive continuation and resume later without a frontend callback.

Each match owns:

- exact profile ID, revision and fingerprint;
- players, current participant and winner state;
- per-player resource balances and current SpaceId;
- generic ownership/status module snapshots and a round number;
- phase, pending immutable decision and continuation state;
- an immutable GameTrack and read-only SpaceView snapshots;
- a match-scoped DeckId collection;
- a match-scoped notification hub;
- one injected randomness boundary and committed DiceRoll outcomes.

Resources are bounded to `0..int.MaxValue`. Mandatory debits use the available
balance; positive overflow rejects the uncommitted transition. Doubles are
observable dice data and have no baseline turn policy. Frontends can read
snapshots and submit validated operations. Presentation notifications are
hints and never control execution.

## JSON and profile files

Core defines the transport-neutral schema semantics. Infrastructure uses
System.Text.Json to parse bytes or bounded streams. `JsonFileGameProfileSource`
opens one explicitly configured file and translates technical failures without
exposing its path. Console owns selection and Core never receives a path.

Before application composition accepts a profile, Core runs the same
`GameSetup.ValidateCompatibility` checks used by `GameSetup.Create`. This
separates source, JSON/schema, semantic-validation and execution-compatibility
failures without constructing or mutating a match.

The distributed original Demo is
[lantern-vale-v1.json](../profiles/demo/lantern-vale-v1.json). Schema and
authoring rules are in [profile-format.md](profile-format.md).

## Persistence boundary

Core owns an immutable `GameStateV2`, exact profile registry and controlled
whole-match reconstruction. Infrastructure owns strict UTF-8 JSON and atomic
file promotion. A save records the profile ID, revision and fingerprint,
ordered players and decks, resources, positions, ownership, round, phase,
decisions, continuation and terminal winner.

Loading resolves the saved identity against an already registered profile and
validates every reference and enabled module before returning a new `Game`.
The API never receives an active match, so failed load cannot partially replace
one. Version 1 is rejected and not migrated.

Runtime randomness, profile paths, handlers, callbacks, logs, subscribers,
notifications and rendered presentation are not state. Restore injects a new
match-scoped random source without consuming it and derives runtime services
from the exact validated profile.

## Frontend boundary

Console resolves profile presentation into terminal-safe projections. It does
not select rules by display text, color or concrete space type. New and loaded
matches enter one line-oriented session runner. It renders current state,
pending decisions and terminal results from immutable Core reads and submits
only the operations exposed by the supported capability baseline.

The ordered route projection assumes no visual geometry. Deck projections show
ordinal deck identity and count without revealing the runtime card order;
drawn-card presentation comes from committed notifications. Notification
callbacks buffer immutable hints and never render or call Core during
notification delivery. Every session owns and disposes its match-scoped
subscription.

The bundled Demo is the default. An explicit `--profile` selection is loaded
before the menu and never falls back to Demo on failure; no private directory
is scanned automatically. `ConsoleWrapper` is the sole terminal boundary, and
untrusted display text is stripped of control characters before output.

## Follow-up ownership

- #40: construct setup from a validated profile (implemented).
- #75: registered capability execution and legacy Core removal (implemented).
- #76: explicitly select external JSON profiles (implemented).
- #52: whole-match Save Format Version 2 (implemented).
- #77: generic playable Console projections (implemented).
- #56: adopt the final neutral project identity.
