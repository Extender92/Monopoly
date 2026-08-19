# Custom rule profile

## Purpose

Custom supports explicit house rules without forking the Core game flow.

A Custom profile must choose UK Classic or US Classic as its base, apply supported overrides, validate the complete result and store that resolved result with the match.

## Design rules

- Custom uses the same `Game`, `PlayTurn()`, board types and transaction operations as Classic.
- Every override is explicit and presentation-neutral.
- An omitted value inherits from the selected base profile.
- The resolved profile becomes immutable when the match starts.
- Save/load persists both profile identity and effective values.
- A frontend may offer valid options but cannot invent or enforce a rule locally.

## Supported setup overrides

The profile model may support:

| Option | Validation |
| --- | --- |
| Player count | At least 2 and within an explicit Core-supported limit that does not depend on a frontend |
| Starting balance | Positive integer |
| Dice count and sides | Positive values with a compatible doubles and Utility policy |
| Starting-player policy | One registered policy, such as highest roll, random or fixed setup order |
| Salary | Non-negative integer |

Classic doubles and Utility behavior require two six-sided dice. A different dice configuration must select rule implementations whose semantics are defined and tested; Core must reject ambiguous combinations.

## Supported flow overrides

The initial Custom catalog may include:

| Option | Classic default | Supported Custom choices |
| --- | --- | --- |
| Rent collection | Owner claim | Owner claim or automatic |
| Property auctions | Required | Required or explicitly disabled |
| Auction opening bid | 10 | Positive integer |
| Minimum bid increase | 1 | Positive integer |
| GO salary | 200 | Non-negative integer |
| Landing exactly on GO | Normal salary | Normal or configured extra salary |
| Jail fine | 50 | Non-negative integer |
| Jail attempts | 3 | Positive integer |
| Mortgage interest | 10% | Non-negative supported percentage |
| Free Parking | No effect | No effect, fixed bank bonus or collected-payment pot |

Collected-payment Free Parking must define exactly which bank payments enter the pot. It must not rely on a frontend convention.

## Building and bank overrides

Custom may expose building supply and valuation only when the complete behavior remains valid:

- Number of Houses and Hotels.
- House/Hotel sale percentage.
- Whether final-building shortages use auctions.

Even building, ownership validation and mortgage restrictions remain enabled unless a separately designed rule option defines their replacement semantics.

## Regional data

Custom initially reuses the complete board and card data from its UK or US base. Individual numeric rule overrides do not copy or mutate the registered Classic definition.

Fully custom boards, new card sets and additional regional editions are separate extensibility features. They require stable identifiers, complete validation and their own documented data source.

## Validation

Validation must reject at least:

- Missing or unknown base profile.
- Unsupported rule identifiers.
- Negative money, bids, inventory or turn limits.
- Player counts below two.
- Dice settings without compatible doubles and Utility semantics.
- A fines pot without a defined set of contributing payments.
- Duplicate board positions or card identities.
- Missing GO, Jail, Go To Jail or other required board spaces.
- A profile that cannot reconstruct the same rules during load.

Errors must identify the invalid option before a match begins.

## Not initially supported

The following need separate design and are not generic Custom toggles:

- Speed Die.
- Short Game or Time Limit Game.
- Fully custom board topology.
- User-authored executable card behavior.
- Private player loans.
- Rules implemented only by one frontend.

## Example resolved profile

```text
Profile: Custom
Base: UK Classic
Starting balance: £1,500
Salary: £200
Rent collection: Automatic
Auctions: Required
Free Parking: Collected-payment pot
Double salary on GO: Enabled
All unspecified values: UK Classic defaults
```

This description is state, not executable logic. Core interprets it through registered rule options.
