# Testing

## Required verification

From the repository root:

~~~text
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release --no-build
~~~

The Release build must have zero warnings and errors. Report passed, failed and
skipped counts. Changes to Console behavior also require an appropriate manual
or wrapper-based smoke test.

## Profile tests

The distributed Lantern Vale file is parsed through the real
JsonGameProfileParser and Core validator. A regression test locks:

- profile ID and revision;
- canonical fingerprint;
- 27 ordered spaces;
- one deck and nine globally unique card IDs;
- group sizes 4, 2, 2, 3 and 3;
- resources, setup and match-end policies;
- absence of statuses and unsupported capabilities.

A semantic profile change requires an incremented revision and a deliberately
updated expected fingerprint.

Small synthetic fixtures cover tracks of different lengths and zero, one and
multiple decks. Parser and validator tests cover security limits, malformed
encodings, unknown fields and kinds, duplicates, broken references, invalid
combinations and canonical fingerprint stability.

No test embeds a complete third-party layout, economy or card collection.

## Match setup tests

The real Lantern Vale JSON is parsed and passed to `GameSetup`. Tests verify
exact profile identity, resources, starting SpaceId, deck state, ownership,
empty status state and round one. Synthetic profiles cover different track
lengths and zero, one and multiple decks.

Scripted randomness proves ordinal deck preparation, independent purpose
sequences, fixed and random starting-player selection, highest-roll tie rerolls
and the bounded tie failure. Setup failures never return a partial match.
Before #75, tests also prove that `PlayTurn` returns the typed execution gate
without changing authoritative or presentation state.

## Runtime regression fixtures

Until #75, existing state-transition tests use a small neutral composition
created only inside the test assembly. It has no production factory and is not
copied as data. It protects movement, payments, decisions, status transitions,
match-scoped notifications and deterministic randomness while the executor is
replaced.

Tests arrange state through controlled test builders and compare detached
snapshots. They do not use retired persistence DTOs.

## Persistence gap tests

Tests prove that save always returns a typed compatibility error without
creating or changing a file. Load tests distinguish retired or unsupported
versions, invalid JSON, missing files and storage failures.

Version 2 round-trip testing belongs to #52.

## Public-boundary tests

Reflection tests verify that exported Core signatures expose no frontend types,
regional factory, edition selector or regional card type. The production
assembly has no default match factory or public constructor that can create the
internal transition runtime.

## Publication checks

Run the clean-publication Audit and its fixture suite for changes that affect
source, data, documentation or artifacts. A Release publish scan must confirm
that the Demo is included and audit/reference material, saves, local profiles
and build intermediates are not included.
