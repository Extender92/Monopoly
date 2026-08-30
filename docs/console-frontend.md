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

> Demo capability execution is available in Core. Interactive match play is
> temporarily unavailable while generic Console projections are being
> completed.

The user acknowledges the message and returns to the menu. Core setup from a
validated profile and the Demo execution baseline are complete, but Console
intentionally does not enter a session until #77. Application composition
explicitly selects and validates the bundled Demo; Core has no default profile.

Loading delegates to the injected IGameSaveStore. During the persistence gap,
the retired format produces the stable unsupported-version message and returns
to the menu without changing a session. Missing, invalid and storage failures
retain their distinct messages.

Saving cannot write a file until #52 introduces Version 2.

## Presentation

Core exposes semantic presentation tokens and immutable metadata. Console maps
known color hints locally and uses safe fallbacks for unknown hints. Text
labels remain available so color is never the only information carrier.

Legacy Console session, action-menu and board-model sources are temporarily
excluded from compilation because their Core compatibility types no longer
exist. Issue #77 deletes those sources and replaces them with generic space,
card, decision and match projections.

## Future composition

Issue #76 adds explicit profile-path selection through Infrastructure. Without
that option, Console will use the distributed Demo. Core will never receive a
file path.

Session lifecycle and subscription cleanup must be reconsidered as neutral
issues after clean publication.
