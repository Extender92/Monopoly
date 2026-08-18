# Monopoly repository instructions

## Purpose

This repository contains the Monopoly game core, the console frontend, and automated tests. Keep the core reusable so future frontends can reference it without depending on console classes.

## Before changing code

- Read the relevant project files and inspect the current Git status and diff first.
- Preserve existing user changes. Do not reset, discard, overwrite, or reformat unrelated work.
- Confirm the change belongs in `Monopoly.Core`, `Monopoly.Console`, or `Monopoly.Tests` before editing.

## Architecture rules

- `Monopoly.Core` is a reusable class library. It must not write to `Console`, call `ReadLine`, pause for user input, or depend on console UI types.
- `Monopoly.Console` owns menus, rendering, input, and mapping core-neutral values to console-specific values.
- Keep game rules in the core. The main turn flow goes through `Game.PlayTurn()` and returns a `TurnResult`.
- Use injected decision providers for frontend choices such as purchasing property, paying to leave jail, and resolving insufficient funds.
- Keep movement, wrapping, landing effects, payment, jail, doubles, bankruptcy, active-player rotation, and winner state in the core.
- Do not add a second console game loop or duplicate rule implementation.
- Events are notifications for presentation and integration only; they must not be the source of truth for game state or turn progression.

## Save and load

- Use the versioned core save format (`Version = 1`).
- Persist IDs and reconstruct references after players, board, rules, and decks have been created.
- Preserve current player, turn/doubles state, fines, jail state, square state, ownership, mortgage state, and card deck order.
- Validate versions, IDs, positions, and collection lengths. Fail clearly for invalid or unsupported saves.
- Keep save/load logic independent of console output and interactive input.

## Testing and verification

- Add or update integration tests for complete game flows when changing rules or state transitions.
- At minimum, run `dotnet build` and `dotnet test` before considering a change complete.
- Do not accept new warnings or failures without explaining them in the handoff.
- Pay special attention to exact-balance payments, correct debt amounts, wrap over Go, jail doubles, third doubles, bankruptcy transfers, winner state, save/load round-trips, and duplicate event subscriptions.

## Git and GitHub workflow

- Never work directly on `main`; create a focused branch such as `feature/...`, `fix/...`, or `refactor/...` from `main`.
- Keep one branch and one pull request focused on one coherent objective.
- Multiple local commits are acceptable while working, but use squash merge so `main` receives one clean logical commit per pull request.
- Use concise commit titles that match the existing repository style. Describe the actual change; do not reuse a closed issue number as if it were being closed again.
- In pull requests, use `Refs #N` or `Related to #N` for partial or follow-up work. Use `Closes #N` only when the issue is completely resolved.
- Run the full build and test suite before opening or merging a pull request.
- Create a Git tag only for a meaningful stable milestone or release, and tag the merged commit on `main`.
- Do not push, merge, close issues, or create releases unless the user explicitly requests that external action.

## Completion checklist

Before reporting completion:

1. Review the final diff for accidental or unrelated changes.
2. Run `dotnet build`.
3. Run `dotnet test`.
4. Report the branch, commit, tests, warnings, and any remaining limitations.
