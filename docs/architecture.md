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

Game is the authoritative match aggregate. Its public construction is
temporarily unavailable: #40 will create a match from a ValidatedGameProfile,
and #75 will execute the declared capabilities and effects.

An internal data-free executor remains only to protect existing state,
decision, randomness and notification regressions. Tests compose it from a
small neutral route and generic deck runtime. It is not a production factory,
default profile or public compatibility layer.

Each match owns:

- players, current participant and winner state;
- phase, pending immutable decision and continuation state;
- an immutable GameTrack and read-only SpaceView snapshots;
- a match-scoped DeckId collection;
- a match-scoped notification hub;
- one injected randomness boundary and committed DiceRoll outcomes.

Frontends can read snapshots and submit validated operations. Presentation
notifications are hints and never control execution.

## JSON and profile files

Core defines the transport-neutral schema semantics. Infrastructure uses
System.Text.Json to parse bytes or bounded streams. File selection belongs to
Infrastructure and Console in #76; Core never receives a path.

The distributed original Demo is
[lantern-vale-v1.json](../profiles/demo/lantern-vale-v1.json). Schema and
authoring rules are in [profile-format.md](profile-format.md).

## Persistence transition

Save Format Version 1 and its DTO mapper have been removed. The injected
IGameSaveStore boundary remains, but the file implementation rejects save and
load with IncompatibleVersion during the intentional gap. It never writes a
file in this state.

Issue #52 owns Version 2. It must bind a save to exact profile ID, revision and
fingerprint and validate the whole reconstructed match before replacing an
active session.

## Frontend boundary

Console resolves profile presentation into terminal-safe projections. It does
not select rules by display text, color or concrete space type in the target
architecture. Full generic projections belong to #77.

During the current gap the Console shell starts, while new and loaded matches
return clear transition messages. It never falls back to bundled legacy data
or a private profile.

## Follow-up ownership

- #40: construct setup from a validated profile.
- #75: register and execute supported capabilities.
- #76: explicitly select external JSON profiles.
- #52: introduce whole-match Save Format Version 2.
- #77: render generic profile projections in Console.
- #56: adopt the final neutral project identity.
