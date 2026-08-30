# Public engine scope

## Purpose

The publication target is a frontend-neutral property-trading engine. It lets a
validated profile define an ordered route, ownable spaces, optional purchases,
usage fees, event decks, bounded effects and a match-ending policy. Core applies
those rules and owns authoritative match state. A frontend only presents state
and submits allowed decisions.

This is a deliberately smaller target than a universal board-game runtime. The
engine does not promise arbitrary maps, free-form scripting, user-provided code,
unbounded components or every mechanic that could appear in a tabletop game.

## Public components

The clean public snapshot contains four responsibility areas:

- **Core** owns validated profile contracts, authoritative match state,
  deterministic transitions, decisions, notifications and save-state meaning.
- **Infrastructure** reads explicitly selected JSON, handles storage technology
  and translates technical failures. It does not decide game rules.
- **Console** is the reference frontend. It renders generic projections and
  submits Core decisions without duplicating rule execution.
- **Profiles** provide declarative data. The repository contains one original
  Demo profile plus small synthetic test profiles.

The dependency direction is:

```text
Console/application -> Core
Console/application -> Infrastructure -> Core contracts
```

Core never receives a file path and never loads executable profile code.

## Supported public model

A validated profile may define:

- an opaque profile identity, revision and canonical fingerprint;
- an ordered route with a profile-defined number of spaces;
- zero or more decks indexed by stable identifiers;
- presentation metadata, setup values and a bounded match-ending policy;
- spaces and cards composed from capabilities supported by the engine; and
- declarative effects with validated identifiers and parameters.

The first public capability baseline is intentionally narrow: movement,
ownership, optional purchase, fixed usage fees, drawing an event, bounded
resource changes and a round-limited score result. Issue #75 owns the exact
executable contract and may reject any capability not registered by the engine.

## Explicit non-goals

The first clean publication does not include:

- an official, compatibility or renamed third-party profile;
- a universal visual board editor or a universal board-game runtime;
- executable scripts, callbacks, CLR type names or profile-provided assemblies;
- remote profile downloads, automatic private-profile discovery or a profile
  marketplace;
- advanced deferred mechanics such as auctions, detention, mortgages,
  buildings, trading, held release cards or bankruptcy chains; or
- a second frontend, public deployment or installer.

Those mechanics may be considered later only as newly scoped work against the
clean architecture. Closed legacy issues are not publication requirements and
are not copied to the new repository.

## Profile trust boundary

JSON profiles are untrusted data. They must pass schema and semantic validation
before a match is created. Validation rejects unknown fields and capabilities,
duplicate identifiers, broken references, incompatible policies and values
outside the published limits. Profiles cannot request code execution.

The versioned wire contract, parser boundary, limits and canonical fingerprint
are documented in [profile-format.md](profile-format.md).

An optional profile selected by a user is local input, not a repository
dependency. Clone, restore, Release build, tests, default Console use and clean
publication must all succeed with only the bundled Demo and synthetic fixtures.
Absolute profile paths must not be written to authoritative saves, logs or
published artifacts.

## Publication boundary

The clean snapshot may contain only reviewed generic engine code, original Demo
content, synthetic fixtures, neutral documentation and approved dependencies.
Legacy audit material and local profiles are excluded even when they are useful
during development. The source and unpacked Release artifacts must pass the
manifest checks from issue #55 before issue #58 can publish them.

This boundary reduces publication risk. It is an engineering and content
control, not legal advice or a guarantee that no third party can make a claim.
