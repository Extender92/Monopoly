# US Classic profile

## Purpose

This document defines the regional identity and defaults supplied by the US Classic profile. Shared behavior is defined in [game-rules.md](../game-rules.md).

## Baseline

The selected baseline is the American Hasbro C1009 Classic rule set published in 2017:

- Publication content: © 2016 Hasbro with 2017 instruction revision.
- Publication code: `C10090000 17 I MN`.
- Players: 2–6.
- Standard game: two six-sided dice.
- [Official Hasbro US instruction PDF](https://www.hasbro.com/common/documents/dad288661c4311ddbd0b0800200c9a66/97DF546B6BF64A70A2A759D3FA5BD804.pdf).

Historical US rulebooks are not combined with this profile. In particular, the older percentage-based Income Tax option and older Luxury Tax values are excluded.

## Defaults

| Rule | US Classic value |
| --- | --- |
| Currency | USD, displayed as `$` |
| Players | 2–6 |
| Starting balance | $1,500 |
| Dice | 2d6 |
| GO salary | $200 |
| Jail fine | $50 |
| Jail doubles attempts | 3 |
| Mortgage interest | 10% |
| Property auction opening bid | $10 |
| Minimum bid increase | $1 |
| Free Parking | No effect |
| Rent collection | Owner claim before next roll |
| Houses and Hotels | 32 Houses, 12 Hotels |
| Winner | Last active player |

## Regional terminology

- Purchasable color-group spaces are **Properties** or **Streets**.
- Transport properties are **Railroads**.
- The two service properties are **Utilities**.
- The second tax space is **Luxury Tax**.

These are domain display names. Stable property and card identifiers must not depend on localized text.

## Board identity

The US board uses the standard 40 positions and Atlantic City names:

| Positions | Group or type | Names |
| --- | --- | --- |
| 1, 3 | Brown | Mediterranean Avenue; Baltic Avenue |
| 6, 8, 9 | Light Blue | Oriental Avenue; Vermont Avenue; Connecticut Avenue |
| 11, 13, 14 | Pink | St. Charles Place; States Avenue; Virginia Avenue |
| 16, 18, 19 | Orange | St. James Place; Tennessee Avenue; New York Avenue |
| 21, 23, 24 | Red | Kentucky Avenue; Indiana Avenue; Illinois Avenue |
| 26, 27, 29 | Yellow | Atlantic Avenue; Ventnor Avenue; Marvin Gardens |
| 31, 32, 34 | Green | Pacific Avenue; North Carolina Avenue; Pennsylvania Avenue |
| 37, 39 | Dark Blue | Park Place; Boardwalk |
| 5, 15, 25, 35 | Railroads | Reading; Pennsylvania; B. & O.; Short Line |
| 12, 28 | Utilities | Electric Company; Water Works |
| 4, 38 | Taxes | Income Tax $200; Luxury Tax $100 |

GO, Community Chest, Chance, Jail/Just Visiting, Free Parking and Go To Jail use their standard positions.

## Cards

The profile contains 16 Chance and 16 Community Chest card identities belonging to the selected American edition. Card text is presentation data; each card maps to a reusable Core effect such as movement, bank payment, player payment, building assessment, jail or Get Out of Jail Free.

The exact deck composition and duplicate cards must be verified as profile data. A card identity may occur only once across its deck and held-card state.

## Profile boundaries

The following are not US Classic defaults:

- Speed Die.
- Percentage-based Income Tax.
- Double salary on GO.
- A Free Parking reward or fines pot.
- Automatic rent collection.
- Disabled property auctions.
- More than 2–6 players.
- Historical or themed board/card data.

They require a validated Custom profile or a separately documented variant.
