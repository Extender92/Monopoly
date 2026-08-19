# Game rules

## Purpose

This document defines the intended rules implemented by `Monopoly.Core`.

It is a normative specification: **must** identifies required behavior, **may** identifies a permitted choice and **Custom may override** identifies an explicit supported house-rule option. Current implementation gaps are tracked in GitHub Issues and are not treated as valid rules here.

Turn orchestration and frontend interaction are described in [game-flow.md](game-flow.md). Regional data and Custom overrides are described in:

- [UK Classic](rules/uk-classic.md)
- [US Classic](rules/us-classic.md)
- [Custom rules](rules/custom.md)

## Official baselines

The project targets modern Hasbro Classic rules rather than combining rules from different historical editions.

| Profile | Baseline | Publication identity |
| --- | --- | --- |
| UK Classic | Hasbro UK C1009 Classic rule set, 2021 | `0622C1009780`, British board and terminology |
| US Classic | Hasbro US C1009 Classic rule set, 2017 | `C10090000 17 I MN`, American board and terminology |

Both selected baselines use the same shared Classic flow: 2–6 players, two six-sided dice, a starting balance of 1,500, salary of 200, required property auctions, owner-claimed rent, Classic jail, finite buildings, trading, mortgages, bankruptcy and last-player-standing victory.

Optional or historical variants are not silently included. In particular, Speed Die, Short Game, Time Limit Game, percentage-based Income Tax and Free Parking jackpots are outside the Classic profiles.

## Rule profiles

Every match uses one resolved and validated profile:

- UK Classic contains immutable official UK defaults and regional data.
- US Classic contains immutable official US defaults and regional data.
- Custom derives from one Classic base and applies supported overrides.

Profiles configure the same Core operations. They do not provide alternate frontend logic or a second turn engine.

The resolved profile must remain stable for the lifetime of a match and must be persisted with save state.

## Components and bank

A Classic match uses:

- One 40-space board.
- Two six-sided dice.
- 16 Chance cards.
- 16 Community Chest cards.
- 28 Title Deeds covering 22 Streets, 4 Stations or Railroads and 2 Utilities.
- 32 Houses.
- 12 Hotels.

The bank owns all unpurchased property, available buildings and money not held by players. Bank money is not a gameplay limit; the building supply is limited.

A frontend may present a human or virtual Banker, but Core owns bank inventory and validates every bank transaction.

## Match setup

For UK Classic and US Classic:

1. Validate that there are 2–6 players.
2. Give each player the profile currency's equivalent of 1,500.
3. Place every token on GO.
4. Shuffle Chance and Community Chest separately.
5. Make all Title Deeds and buildings available to the bank.
6. Have every player roll both dice.
7. The highest roller starts; tied highest players reroll until the tie is resolved.
8. Play proceeds in player order to the left, represented by the configured player order.

Opening rolls do not move tokens, count as doubles or start a player turn.

## Player turns and doubles

On a normal roll/action cycle:

1. Roll both dice.
2. Move clockwise by their total.
3. Resolve the complete landing chain.
4. If the roll was doubles and the player remains active and out of jail, the player rolls again.
5. Otherwise the turn passes to the next active player.

Doubles are consecutive only within the same player turn. On the third consecutive doubles, the player goes directly to jail, does not move by the third total and ends the turn.

## Movement and GO

Forward movement wraps around the board. A player receives 200 whenever an eligible movement lands on or passes GO. A movement that completes multiple laps awards salary once per completed lap.

Card instructions may award salary differently and take precedence for that movement. Moving directly to jail never awards salary for crossing GO.

Backward movement does not award salary merely because its normalized position crosses position zero.

Landing on the Jail corner through ordinary movement means Just Visiting and has no jail effect.

## Transaction windows

Classic allows eligible transactions at any time, including while a player is in jail. A digital game represents this with safe transaction windows between atomic Core operations.

During a transaction window, eligible players may:

- Buy or sell buildings.
- Mortgage or unmortgage property.
- Propose and accept deals.
- Participate in an auction.

Core must validate the acting player, ownership, funds, inventories and resulting state. A frontend menu is never the only enforcement layer.

An unresolved mandatory decision, debt, rent claim, auction or bankruptcy settlement must be completed before the next dice roll.

## Unowned property and purchase

When movement or a card lands a player on an unowned Street, Station or Railroad, or Utility, the player may buy it from the bank for the printed price.

The purchase decision occurs before optional asset management. If the player accepts, they may raise cash through otherwise legal transactions. Failure to fund an optional purchase is not bankruptcy.

If the player declines or cannot complete the purchase, the bank must auction the property.

## Property auctions

Classic auctions follow these rules:

- Every active player may bid, including the player who declined the printed-price purchase and players in jail.
- Bidding starts at 10.
- A new bid increases the current bid by at least 1.
- Turn order is not required for bidding.
- The auction ends when no participant is willing to increase the highest valid bid.
- The winner pays the bank and receives the Title Deed.
- If nobody bids, the property remains with the bank.

Core owns auction state, validates available cash and prevents invalid or endless bidding.

## Owned property and rent claims

Landing on unmortgaged property owned by another player creates a potential rent claim.

The owner must claim rent before the next player rolls. If the owner claims it, the landing player must pay the calculated amount. If the owner waives it or the claim expires, no rent is owed for that landing.

Players do not pay themselves. Mortgaged property collects no rent.

### Streets and color sets

Owning every Street in a color set doubles the printed unimproved rent on each unmortgaged Street in that set.

The increased unimproved rent remains available on an unmortgaged Street even when another Street in the completed set is mortgaged. Buildings may not be added while any Street in the set is mortgaged.

Building rent is taken from the Title Deed for the exact building level.

### Stations and Railroads

Rent depends on how many Stations or Railroads the owner holds:

| Owned | Rent |
| ---: | ---: |
| 1 | 25 |
| 2 | 50 |
| 3 | 100 |
| 4 | 200 |

Card instructions may require double the otherwise applicable rent.

### Utilities

When rent is claimed on a Utility, Core makes a separate rent roll with both dice. This roll does not move the player, count as doubles or alter turn state.

- One Utility owned: rent is four times the rent roll.
- Both Utilities owned: rent is ten times the rent roll.

A card that sends a player to the nearest Utility may specify ten times a new dice roll regardless of how many Utilities the owner holds.

## Houses and Hotels

A player may build only after owning every Street in a color set. No Street in the set may be mortgaged.

Houses must be built evenly:

- A second House may not be placed on a Street until every Street in the set has one.
- The same rule applies at each level.
- A Street may contain at most four Houses.

A Hotel may be bought only after every Street in the set has four Houses. Buying a Hotel returns four Houses from that Street to the bank and places one Hotel. A Street may contain at most one Hotel.

Building costs come from the Title Deed. The bank has only 32 Houses and 12 Hotels:

- If the required building is unavailable, it cannot be bought.
- If multiple players want the final available House or Hotel, the bank auctions it with a starting bid of 10 and minimum increase of 1.

### Selling buildings

Buildings are sold only to the bank.

- A House returns half its printed cost.
- A Hotel returns half its printed Hotel cost and is exchanged for four Houses when the bank can supply them.
- Buildings must be sold evenly across the color set, reversing the even-building rule.
- Buildings cannot be sold or transferred directly to another player.

## Deals and trades

Active players may buy, sell or exchange eligible assets by mutual agreement during a transaction window, including while in jail.

A deal may contain:

- Money.
- Unimproved property.
- Get Out of Jail Free cards.

Before a Street in a color set can be traded, every building in that color set must be sold to the bank. Buildings themselves cannot be traded.

Mortgaged property may be traded. The receiving player must immediately either:

- Pay the full unmortgage cost; or
- Keep it mortgaged and pay the bank 10% of its mortgage value.

If kept mortgaged, a later unmortgage still costs the mortgage principal plus 10%.

Players may not create private loans or agreements to waive future rent.

## Mortgages

Only property owned by the acting player may be mortgaged. Before mortgaging a Street, all buildings in its color set must be sold evenly to the bank.

Mortgaging pays the printed mortgage value and turns the Title Deed face down. No rent is collected on that property.

Unmortgaging costs the printed mortgage value plus 10%. Mortgage interest and transfer charges are bank payments and use the normal insufficient-funds rules when mandatory.

## Action spaces

### Chance and Community Chest

Draw the top card, apply its complete effect immediately and return it to the bottom of its originating deck.

A Get Out of Jail Free card is the exception: it remains out of its deck while held. It returns to the bottom of the correct deck after use or bank bankruptcy and may transfer through a legal deal or player-creditor bankruptcy.

Card movement uses the shared movement and landing flow. Card payments use the stated creditor and amount. A payment to every other player is a set of player debts, not a bank debt.

Multi-player card obligations use active player order, beginning with the first
active player after the card actor and wrapping once around the saved player
order. For a card that pays every other player, the actor settles one recipient
at a time in that order until all obligations are paid or the actor becomes
bankrupt. For a card that collects from every other player, each payer settles
independently in the same order; one payer's bankruptcy does not skip the
remaining payers unless the match has ended. Players who were already bankrupt
when the card was drawn are not participants.

### Taxes

Income Tax and the regional second tax charge the printed amount to the bank. They do not fund Free Parking in a Classic profile.

### Free Parking

Nothing happens in UK Classic or US Classic. The player takes their next turn normally.

### Go To Jail

Move directly to the In Jail area without salary. The current turn ends immediately.

## Jail

A player goes to jail by:

- Landing on Go To Jail.
- Drawing a Go To Jail card.
- Rolling three consecutive doubles in one turn.

A jailed player may still collect claimed rent, bid in auctions, build, mortgage and trade.

At the start of a jail turn before rolling, the player may pay 50 or use a held Get Out of Jail Free card, then roll and move normally. Doubles after a pre-roll release follow the normal doubles rule.

Alternatively, the player may attempt to roll doubles for up to three turns:

- Successful doubles release and move the player but end the turn without an extra roll.
- A failed first or second attempt leaves the player in jail and ends the turn.
- After a failed third attempt, the player must pay 50 and use that third roll to move.

Failure to pay a mandatory jail fine enters the normal insufficient-funds and bank-bankruptcy flow.

## Insufficient funds

When a mandatory debt exceeds available cash, the debtor may raise money through valid building sales, mortgages and accepted deals.

Core repeatedly validates actual economic progress. If total eligible assets cannot cover the debt, or the player makes no progress, bankruptcy replaces further decision requests.

The original debt is transferred exactly once after sufficient cash is available. Optional purchases and auction bids are not mandatory debts.

## Bankruptcy

### Debt to another player

Buildings are returned to the bank for their bankruptcy value. The creditor receives the proceeds, remaining money, all properties and held Get Out of Jail Free cards.

Mortgage state follows transferred property. The creditor immediately pays 10% of each transferred mortgage or pays the full unmortgage cost.

### Debt to the bank

Buildings return to the bank, properties return to the bank and mortgages are canceled. Held Get Out of Jail Free cards return to their originating decks. The bank immediately auctions every returned property.

### Completion

The debtor is removed from active order only after settlement is internally consistent. A bankruptcy advances player order at most once.

## Winning

The match ends when only one active player remains. That player is the winner.

Short-game scoring and time-limit scoring are separate variants and are not part of UK Classic or US Classic.

## Classic invariants

- All mandatory rules execute in Core.
- UK and US use one shared rule engine.
- Regional names and card data do not change dependency direction.
- Classic defaults are not modified during a match.
- House rules exist only in an explicit Custom profile.
- A frontend cannot bypass ownership, payment, building, mortgage, auction, jail or bankruptcy validation.
- No unresolved mandatory action is skipped by starting another roll.
