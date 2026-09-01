# Legacy property-trading engine

> **Development status:** This legacy repository is an unofficial work in
> progress. It provides no supported release or deployment artifact.

[![Build and tests](https://github.com/Extender92/Monopoly/actions/workflows/Build-And-Tests.yml/badge.svg?branch=main)](https://github.com/Extender92/Monopoly/actions/workflows/Build-And-Tests.yml)
[![Spelling](https://github.com/Extender92/Monopoly/actions/workflows/SpellChecker.yml/badge.svg?branch=main)](https://github.com/Extender92/Monopoly/actions/workflows/SpellChecker.yml)

A .NET 10 and C# 14 codebase being transformed into a frontend-neutral
property-trading engine. The public target is generic Core contracts, a
project-owned Demo profile and small synthetic tests.

## Current status

The tracked Demo profile is
[Lantern Vale](profiles/demo/lantern-vale-v1.json). Infrastructure parses its
strict JSON and Core validates it into an immutable ValidatedGameProfile with a
canonical SHA-256 fingerprint.

The profile transition now has an executable Core baseline and a playable
reference frontend:

- Core creates and runs matches from an explicitly supplied validated profile.
  Movement, purchase decisions, fixed usage fees, generic draws, bounded
  effects, rounds and terminal scoring use one registered execution path.
- Console uses the bundled Demo by default or loads one explicitly selected
  local JSON profile with `--profile`. It creates players, runs turns and
  decisions, renders linear generic state, and saves or resumes Save V2.
- Save Format Version 2 atomically stores the complete supported match state
  and requires an exact registered profile ID, revision and fingerprint when
  loading. Version 1 remains retired and is not migrated.
- No legacy Core rule executor or product-shaped runtime state remains.

No external or private profile is required by clone, build, tests or default
Console use.

## Architecture

- Monopoly.Core owns profile contracts, validation, authoritative match state
  and state transitions.
- Infrastructure owns strict JSON parsing and technical storage boundaries.
- Monopoly.Console owns terminal input and presentation.
- Monopoly.Tests uses the original Demo and small neutral runtime fixtures.

Authoritative identities use ProfileId, SpaceId, DeckId and CardId.
Presentation is resolved separately through PresentationToken. Tracks have no
fixed length and deck collections may contain zero, one or many decks.

See [Architecture](docs/architecture.md) and
[Public engine scope](docs/public-engine-scope.md).

## Build and test

Prerequisites are Git and the stable .NET 10 SDK selected by
[global.json](global.json).

~~~text
git clone https://github.com/Extender92/Monopoly.git
cd Monopoly
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release --no-build
~~~

Run the Console reference frontend with:

~~~text
dotnet run --project Monopoly.Console
~~~

Select one local profile explicitly with:

~~~text
dotnet run --project Monopoly.Console -- --profile <path>
~~~

The path may be relative or absolute. Console loads it once before opening the
menu, does not scan profile directories and does not fall back to the Demo when
an explicitly selected profile fails validation or compatibility checks.

The session uses numbered line-oriented commands. Its route view supports any
validated track length. Deck views show names and card counts; only a card that
has actually been drawn is revealed.

## Documentation

- [Public engine scope](docs/public-engine-scope.md)
- [Demo profile design](docs/demo-profile-design.md)
- [JSON profile format](docs/profile-format.md)
- [Architecture](docs/architecture.md)
- [Game flow](docs/game-flow.md)
- [Capability baseline](docs/game-rules.md)
- [Capability execution](docs/capability-execution.md)
- [Save and load](docs/save-format.md)
- [Console frontend](docs/console-frontend.md)
- [Testing](docs/testing.md)
- [Development workflow](docs/development-workflow.md)

GitHub Issues are the source of truth for planned work. Issue #59 defines the
legacy repository's execution order.

## Contributing

Read [Development workflow](docs/development-workflow.md), start significant
work from a focused issue and use a dedicated branch. The clean publication
process and future project identity are tracked separately.
