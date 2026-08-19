# UK Classic profile

## Purpose

This document defines the regional identity and defaults supplied by the UK Classic profile. Shared behavior is defined in [game-rules.md](../game-rules.md).

## Baseline

The selected baseline is the modern British Hasbro C1009 Classic rule set:

- Publication content: © 2021 Hasbro.
- Publication code: `0622C1009780`.
- Players: 2–6.
- Standard game: two six-sided dice.
- [Official Hasbro instruction download](https://instructions.hasbro.com/api/download/C1009_en-us_monopoly-board-game-for-ages-8-for-2-6-players-includes-8-tokens-tokens-may-vary.pdf).

The linked route is locale-labelled `en-us`, but the PDF content is the British edition: it uses London Streets, Railway Stations, pounds, Super Tax and Hasbro UK publication information. The profile is identified by publication content and code rather than URL locale.

The Speed Die from some earlier UK packages is not part of this profile.

## Defaults

| Rule | UK Classic value |
| --- | --- |
| Currency | GBP, displayed as `£` |
| Players | 2–6 |
| Starting balance | £1,500 |
| Dice | 2d6 |
| GO salary | £200 |
| Jail fine | £50 |
| Jail doubles attempts | 3 |
| Mortgage interest | 10% |
| Property auction opening bid | £10 |
| Minimum bid increase | £1 |
| Free Parking | No effect |
| Rent collection | Owner claim before next roll |
| Houses and Hotels | 32 Houses, 12 Hotels |
| Winner | Last active player |

## Regional terminology

- Purchasable color-group spaces are **Streets**.
- Transport properties are **Railway Stations**.
- The two service properties are **Utilities**.
- The second tax space is **Super Tax**.

These are domain display names. Stable property and card identifiers must not depend on localized text.

## Board identity

The UK board uses the standard 40 positions and London names:

| Positions | Group or type | Names |
| --- | --- | --- |
| 1, 3 | Brown | Old Kent Road; Whitechapel Road |
| 6, 8, 9 | Light Blue | The Angel, Islington; Euston Road; Pentonville Road |
| 11, 13, 14 | Pink | Pall Mall; Whitehall; Northumberland Avenue |
| 16, 18, 19 | Orange | Bow Street; Marlborough Street; Vine Street |
| 21, 23, 24 | Red | Strand; Fleet Street; Trafalgar Square |
| 26, 27, 29 | Yellow | Leicester Square; Coventry Street; Piccadilly |
| 31, 32, 34 | Green | Regent Street; Oxford Street; Bond Street |
| 37, 39 | Dark Blue | Park Lane; Mayfair |
| 5, 15, 25, 35 | Railway Stations | King's Cross; Marylebone; Fenchurch Street; Liverpool Street |
| 12, 28 | Utilities | Electric Company; Water Works |
| 4, 38 | Taxes | Income Tax £200; Super Tax £100 |

GO, Community Chest, Chance, Jail/Just Visiting, Free Parking and Go To Jail use their standard positions.

## Cards

The profile contains 16 Chance and 16 Community Chest card identities belonging to the selected British edition. Card text is presentation data; each card maps to a reusable Core effect such as movement, bank payment, player payment, building assessment, jail or Get Out of Jail Free.

The exact deck composition and duplicate cards must be verified as profile data. A card identity may occur only once across its deck and held-card state.

## Profile boundaries

The following are not UK Classic defaults:

- Speed Die.
- Double salary on GO.
- A Free Parking reward or fines pot.
- Automatic rent collection.
- Disabled property auctions.
- More than 2–6 players.
- Alternative London board data from another edition.

They require a validated Custom profile or a separately documented variant.
