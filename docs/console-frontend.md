# Console frontend

## Responsibility

Console is a reference frontend shell. It owns terminal input, navigation,
colors and rendering. Core owns profile validation, decisions and match state.
Infrastructure owns JSON and file access.

Console must never select rules by display text, hard-code profile data or use
a private profile as a fallback.

## Current WIP behavior

The application starts and displays its menu.

Selecting a new match displays:

> The selected profile is valid and supported. Interactive match play is
> temporarily unavailable while generic Console projections are being
> completed.

The user acknowledges the message and returns to the menu. Core setup from a
validated profile and the Demo execution baseline are complete, but Console
intentionally does not enter a session until #77. Application composition uses
the bundled Demo unless the process received an explicit profile path; Core has
no default profile.

The supported commands are:

~~~text
Monopoly.Console [--profile <path>] [--help]
~~~

`--profile` accepts one relative or absolute file path. The file is loaded,
semantically validated and checked against the current execution registry
before the menu opens. Failure exits without creating application or match
state and never falls back to Demo. Paths are not copied into Core, errors,
logs or saves.

Loading delegates to the injected IGameSaveStore with a registry containing
exactly the profile selected at process start. Save V2 reconstructs a complete
match only when ID, revision and fingerprint match. A valid loaded match still
returns to the menu until #77 supplies the generic session. Missing, invalid,
unsupported-version, incompatible-profile and storage failures retain distinct
safe messages.

Infrastructure can atomically write Save V2. The compiled WIP shell has no
active session or save command until #77; save naming and discovery are not
part of the current baseline.

## Presentation

Core exposes semantic presentation tokens and immutable metadata. Console maps
known color hints locally and uses safe fallbacks for unknown hints. Text
labels remain available so color is never the only information carrier.

Legacy Console session, action-menu and board-model sources are temporarily
excluded from compilation because their Core compatibility types no longer
exist. Issue #77 deletes those sources and replaces them with generic space,
card, decision and match projections.

## Profile composition

Infrastructure distinguishes missing, denied, invalid-path and storage errors.
JSON/schema errors, semantic profile errors and unsupported execution
components retain their separate typed boundaries. Console maps those to safe
messages without displaying the source path.

The selected `ValidatedGameProfile` is injected into the menu and retained for
the next new match. Issue #77 owns player input, actual match creation and the
interactive session.

Session lifecycle and subscription cleanup must be reconsidered as neutral
issues after clean publication.
