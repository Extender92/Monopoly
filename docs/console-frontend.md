# Console frontend

## Responsibility

Console is the playable reference frontend. It owns terminal input,
line-oriented navigation, presentation resolution and user-facing errors. Core
owns profile validation, decisions and authoritative match state.
Infrastructure owns profile JSON and Save V2 file access.

Console never selects a rule by display text, color or concrete space/card
type. Unsupported capabilities are rejected by Core compatibility validation
before the menu opens.

## Startup and profile selection

The supported commands are:

~~~text
Monopoly.Console [--profile <path>] [--help]
~~~

Without `--profile`, application composition uses the bundled Lantern Vale
Demo. An explicit relative or absolute path selects exactly that file. Source,
JSON, semantic or compatibility failure exits without creating a menu or match
and never falls back to Demo. Paths do not enter Core, saves, logs or messages.

The selected immutable profile is retained for both new and loaded matches.
New setup asks for a player count within the profile limits and a non-empty name
for each player. Input order defines player IDs `0..n-1`; display names need not
be unique. Blank input cancels setup without creating partial match state.

## Session flow

New and loaded matches enter the same session runner. A ready match offers:

- play the next turn;
- inspect the ordered route;
- inspect deck names and counts;
- save to the temporary fixed `game_data.json` destination; or
- return to the main menu without implicit saving.

An awaiting match displays the immutable Core decision and only its allowed
responses. The submitted response uses the exact decision ID and participant
ID. Route, deck, explicit save and return commands remain available while a
decision is pending. A terminal match displays its winner and can be inspected
or saved before returning.

Each session subscribes to its match-scoped notification source. The callback
only buffers immutable notifications; formatting and terminal writes occur
after the Core operation returns. The subscription is disposed whenever the
session exits. Returning to the main menu never invokes another menu
recursively and never creates a replacement match.

## Generic projections

The main projection displays profile, phase, round, current participant, last
dice result, player positions and profile-ordered resources. The route view
lists spaces in authoritative track order with participants, ownership and the
supported generic capability data such as purchase price, fixed usage fee and
draw-deck reference. It assumes no geometric board or fixed track length.

Decks are sorted ordinally by `DeckId` and show display name plus card count.
The current order and upcoming cards are intentionally hidden. A card's text is
shown only after `CardDrawnNotification`. Movement, resource, ownership,
decision, card, turn and winner notifications render in their committed order.

Required profile, space, resource, deck and drawn-card presentation tokens must
resolve. A missing required token aborts the frontend session with a clear
projection error. Optional or unknown color hints use white; layout hints do
not alter the linear view. Lantern Vale accent tokens have frontend-local
terminal colors, while labels remain the primary information carrier.

All untrusted presentation and loaded player text is stripped of terminal
control characters before rendering. `ConsoleWrapper` is the only production
type that accesses `System.Console`.

## Persistence and limits

Load registers only the profile selected at process start. Save V2 requires an
exact profile ID, revision and fingerprint and reconstructs ready, pending and
terminal matches without consuming randomness. Missing, invalid,
unsupported-version, incompatible-profile and storage failures have distinct
safe messages.

Save naming, save discovery, profile editing, advanced deferred mechanics, a
visual ASCII board and adaptive terminal-region rendering are not part of this
reference baseline. They may be planned independently after clean publication.
