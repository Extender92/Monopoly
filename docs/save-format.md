# Save and load

## Version 2 contract

Save Format Version 2 is the only supported format. Its tracked Draft 2020-12
schema is [game-save-v2.schema.json](../schemas/game-save-v2.schema.json).
Version 1 is intentionally rejected and is not migrated.

The root contains `formatVersion`, an exact profile reference and the complete
supported match state. JSON uses camelCase, UTF-8 without a required BOM, a
maximum size of 5 MiB and maximum depth 64. Unknown or duplicate members,
comments, trailing commas and wrong wire types are invalid.

The profile reference consists of:

- `ProfileId`;
- positive `ProfileRevision`;
- canonical lowercase SHA-256 `ProfileFingerprint`.

Load resolves all saved IDs against an already registered
`ValidatedGameProfile`. A matching ID and revision with a different fingerprint
is not considered compatible.

## Authoritative match state

Version 2 stores:

- players in their authoritative cyclic order, with ID, name, current
  `SpaceId` and every profile resource balance;
- current player, round anchor, round number, phase and optional winner;
- the last committed turn-dice results;
- every profile deck and its current ordered `CardId` sequence;
- version 1 ownership state for every ownable `SpaceId`;
- version 1 status state, which must currently be empty;
- an optional purchase decision and its primitive continuation;
- all consumed decision IDs and the most recently consumed ID.

The numeric board position is derived from `SpaceId` and is not duplicated in
the file. A continuation reuses the top-level committed dice outcome, so the
same roll is not serialized twice.

The current capability baseline supports only a pending purchase decision. Its
saved kind, participant, allowed responses, space and resource price must match
the registered profile. The continuation must point immediately after that
space's purchase capability. Resume never replays a preceding draw, effect or
movement.

## Whole-match validation

Infrastructure parses the untrusted wire document. Core then validates the
entire detached candidate before returning a `Game`:

- roster size and all player references;
- complete, non-negative resource sets;
- positions against the registered track;
- complete ownership and deck state without duplicate or missing IDs;
- phase, round, winner and scoring/tie-break consistency;
- pending, continuation, consumed and stale-decision invariants;
- supported module versions and execution compatibility.

Restore creates decks without shuffling and consumes no random input. A new
match-scoped random source is attached for future turns. Logs, notifications,
subscribers and presentation are freshly derived runtime state. A restored
terminal match has a completed notification boundary.

The load API does not accept an active match. It returns a new match only after
validation, so malformed input cannot partially mutate the caller's current
session.

## Storage and errors

Core owns `GameStateV2`, the immutable profile registry, validation and
controlled reconstruction. Infrastructure owns JSON, file paths and atomic
physical writes. Console owns the selected profile and user-facing messages.

Save serializes and validates before opening a file. Infrastructure writes a
unique temporary file in the destination directory, flushes it to disk and
then atomically replaces an existing target or moves it into place. A write,
flush or promotion failure preserves the previous valid target and performs
best-effort temporary cleanup.

Stable store categories are:

- `NotFound` for an absent file;
- `InvalidData` for malformed JSON or inconsistent Version 2 state;
- `IncompatibleVersion` for Version 1, unknown formats or unsupported module
  versions;
- `IncompatibleProfile` when the exact saved profile is not registered;
- `StorageFailure` for technical read/write failures.

Runtime random sources, seeds, source paths, callbacks, handlers, subscribers,
logs, notifications and rendered presentation are never serialized. Committed
dice results, selected starting/round-anchor participant and deck order contain
the authoritative consequences of earlier randomness.

The current Console composition registers exactly the bundled or explicitly
selected profile. It continues to use `game_data.json` until save naming and
selection are planned in the clean-root project.
