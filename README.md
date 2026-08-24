# Monopoly

> **Development status:** This repository is an unofficial work in progress.
> No supported releases or deployment artifacts are provided.

[![Build and tests](https://github.com/Extender92/Monopoly/actions/workflows/Build-And-Tests.yml/badge.svg?branch=main)](https://github.com/Extender92/Monopoly/actions/workflows/Build-And-Tests.yml)
[![Spelling](https://github.com/Extender92/Monopoly/actions/workflows/SpellChecker.yml/badge.svg?branch=main)](https://github.com/Extender92/Monopoly/actions/workflows/SpellChecker.yml)

A .NET 10 and C# 14 Monopoly implementation built around a reusable game Core.
The repository contains the authoritative game engine, a playable Console
frontend and automated tests.

The long-term goal is one frontend-neutral Core that can power Console, web,
desktop, mobile or game-engine frontends without duplicating Monopoly rules.

## Project status

The Console application is playable and demonstrates the current Core game
flow. The project remains under active development: some Classic rules,
UK/US/Custom rule profiles and frontend-neutral infrastructure are designed and
documented but are not yet completely implemented.

The documentation distinguishes normative target behavior from sections marked
as the current implementation. Current defects and planned work are tracked in
[GitHub Issues](https://github.com/Extender92/Monopoly/issues).

## Architecture

`Monopoly.Core` is the game. It owns authoritative match state, rules and state
transitions. Frontends own presentation, input and framework integration.

```text
Monopoly.Console ──> Monopoly.Core
Monopoly.Tests   ──> Monopoly.Core
Monopoly.Tests   ──> Monopoly.Console
```

A frontend decides how a choice is presented and collected. Core decides which
choices are allowed and what each accepted choice does to the match.

See [Architecture](docs/architecture.md) for the complete dependency and
responsibility model.

## Repository structure

- [Monopoly.Core](Monopoly.Core/) – reusable class library containing game state
  and rules.
- [Monopoly.Console](Monopoly.Console/) – playable reference frontend using
  terminal input and rendering.
- [Monopoly.Tests](Monopoly.Tests/) – Core integration, unit and Console
  component tests.
- [docs](docs/) – architecture, rules, persistence, frontend, testing and
  workflow documentation.

Future projects are added only when their implementation begins; planned web or
Infrastructure boundaries are documented without being presented as existing
projects.

## Prerequisites

- Git.
- A stable [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

The repository's [global.json](global.json) selects SDK `10.0.201`, allows
later stable .NET 10 feature bands and excludes preview SDKs.

## Clone, build and test

```text
git clone https://github.com/Extender92/Monopoly.git
cd Monopoly
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release --no-build
```

The full verification requirements are documented in
[Testing](docs/testing.md).

## Run the Console frontend

From the repository root:

```text
dotnet run --project Monopoly.Console
```

The interactive frontend uses terminal cursor positioning and colors. Use a
terminal that supports those capabilities.

Current menu controls:

- Arrow keys move the selection.
- Enter accepts the highlighted option.
- Escape cancels where cancellation is available.

See [Console frontend](docs/console-frontend.md) for session behavior, manual
smoke testing and the boundary between UI and game logic.

## Documentation

- [Architecture](docs/architecture.md) – project boundaries and target design.
- [Game flow](docs/game-flow.md) – setup, turns, decisions and match completion.
- [Game rules](docs/game-rules.md) – shared rules and UK, US and Custom profiles.
- [Save and load](docs/save-format.md) – persistence contract, Version 1 and
  future requirements.
- [Console frontend](docs/console-frontend.md) – input, rendering and Core
  integration.
- [Testing](docs/testing.md) – automated strategy, CI and manual verification.
- [Development workflow](docs/development-workflow.md) – issues, branches,
  pull requests, squash merges and releases.

## Contributing

Start significant work from a focused GitHub issue and a branch based on the
latest `main`. Changes are delivered through pull requests and squash merged
after the required checks pass.

Read [Development workflow](docs/development-workflow.md) before preparing a
change. Use [GitHub Issues](https://github.com/Extender92/Monopoly/issues) for
defects, proposals and questions about planned work.

## Attribution

This repository continues work originally developed in
[CodeCraftersMR/CCMR-Monopoly](https://github.com/CodeCraftersMR/CCMR-Monopoly).
