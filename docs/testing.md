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

Profile-source and Console-composition tests cover bundled default selection,
explicit relative and absolute paths, argument validation, safe technical file
errors, JSON/schema failures, semantic validation and execution compatibility.
They verify that explicit failure starts neither menu nor match, never falls
back to Demo and never exposes an absolute path.

Fake-wrapper Console tests cover player registration, cancellation, new/load
composition, ready/pending/terminal sessions, purchases, safe rejections,
explicit saves and subscription cleanup. Projection tests cover arbitrary
linear tracks, resources, ownership, zero/one/multiple decks, hidden future
card order, drawn-card text, structured notification order and neutral
presentation fallback. Source-contract tests require `ConsoleWrapper` to be the
only `System.Console` caller and reject removed product-shaped Console paths.

No test embeds a complete third-party layout, economy or card collection.

## Match setup tests

The real Lantern Vale JSON is parsed and passed to `GameSetup`. Tests verify
exact profile identity, resources, starting SpaceId, deck state, ownership,
empty status state and round one. Synthetic profiles cover different track
lengths and zero, one and multiple decks.

Scripted randomness proves ordinal deck preparation, independent purpose
sequences, fixed and random starting-player selection, highest-roll tie rerolls
and the bounded tie failure. Setup failures never return a partial match.
Execution tests prove profile dice, origin passes, decisions, ownership,
bounded fees, deck rotation, effects, rounds and terminal scoring.

## Full Demo scenario

The setup-to-winner scenario parses the tracked Lantern Vale fixture and drives
one two-player match for exactly 12 rounds through the public action and
decision contracts. Scripted dice cover an accepted purchase, a usage fee,
declined purchases, every Demo card, relative and absolute movement,
pass-origin rewards, ordered resource changes and one terminal winner.

After every committed phase, the scenario compares runtime state with the
transport-neutral Save V2 projection. It also verifies structured notification
order, exact deck rotation and mutation-free duplicate-decision rejection. The
complete scenario is run twice and compared semantically. Opaque decision GUIDs
are mapped by creation order because they are authoritative within one match
but intentionally differ between independently created matches.

The scenario reads only the tracked Demo fixture copied to test output. It uses
no external or private profile, mutable save, temporary file, network, sleep or
Console input. Existing one-space/zero-deck and four-space/two-deck fixtures
provide the structural counterexamples to the 27-space/one-deck Demo.

## Runtime execution fixtures

Legacy executor tests were removed with their unsupported runtime. The
replacement suite uses the real Demo and small synthetic validated profiles.
It covers tracks of 1, 2, 3, 4, 27 and 53 spaces; zero, one and multiple decks;
movement, purchases, bounded fees, resource effects, resumable decisions,
rounds, terminal scoring, atomic failures and match isolation.

Effect-chain fixtures cover multiple ordered moves and resource changes,
acyclic nested draws across one or more decks, exact deck rotation, final
landing capabilities and sequential purchase boundaries. Self-loops and longer
possible Draw cycles are rejected before setup. Late overflow tests compare the
entire match snapshot and notification stream to prove atomic rollback.

Purchase tests distinguish initially unaffordable landings from accept
responses that become unaffordable after a decision was created. They also
cover exact one-time acceptance, Demo decline, malformed, disallowed, stale,
duplicate and wrong-participant responses, changed decision preconditions and
post-commit notifications. Synthetic trusted-registry tests prove that a
decline policy may request only a declared, independently registered capability
and that missing, unexpected or failing dispatch cannot commit match state.

The pre-#75 baseline contained 289 tests, many of which asserted removed
detention, building, mortgage, special-property and legacy persistence
behavior. Test-count comparison is therefore not a compatibility measure;
supported contracts must be traced to the replacement profile tests.

## Persistence tests

Save V2 tests round-trip fresh, pending-decision, resumed and terminal matches
through the physical JSON adapter. Synthetic fixtures cover non-default player,
space, resource, deck and card IDs and zero, one or several decks. Restore must
not consume the injected random source; subsequent scripted execution remains
deterministic.

Negative tests cover retired and unknown versions, exact-profile mismatch,
malformed or oversized UTF-8, duplicate/unknown fields, broken references,
incomplete resources/decks/ownership, invalid decisions and terminal state and
unsupported module versions. Atomic-write doubles fail creation, write, flush
and promotion while proving that the previous valid file survives and temporary
state is cleaned.

## Public-boundary tests

Reflection tests verify that exported Core signatures expose no frontend
types, regional factory, edition selector, concrete legacy space/card type,
rules object or alternate executor. The production assembly has no default
match factory or public constructor that bypasses validated setup.

## Publication checks

Run the clean-publication Audit and its fixture suite for changes that affect
source, data, documentation or artifacts. A Release publish scan must confirm
that the Demo is included and audit/reference material, saves, local profiles
and build intermediates are not included.
