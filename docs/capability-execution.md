# Capability execution

## Runtime contract

`GameSetup` validates one closed component registry before a match is returned.
The same registrations dispatch every runtime capability, effect and policy;
profiles cannot supply code or select CLR types.

Each action executes against detached player, position, ownership and deck
state. Core validates the complete prepared transition before committing it.
Notifications are published after commit and subscriber failures never affect
rules. A random-source or resource-overflow failure can consume runtime random
input, but it cannot commit match state.

## Turn movement

- **Preconditions:** the match is `ReadyForTurn`, has no pending decision and
  has not ended.
- **Mutation:** Core rolls the profile dice with `TurnDice`, moves forward on
  the ordered track and commits the roll and final position.
- **Origin policy:** a positive movement applies the configured reward once per
  complete track crossing. Backward movement never earns it.
- **Decision:** none.
- **Failure:** invalid randomness or positive resource overflow rejects the
  uncommitted turn.
- **Notifications:** committed resource changes and player movement, followed
  by the resolved landing.

Doubles remain part of `DiceRoll`; schema version 1 has no extra-turn policy.

## Landing capabilities

Landing handlers run in the fixed registry order `Ownable`, `Purchasable`,
`UsageFee`, then `Draw`.

### Ownable

- Setup creates one empty ownership entry for each declared ownable space.
- Landing itself performs no mutation or decision.
- Ownership is stored by `SpaceId`, separately from presentation and board
  structure.

### Purchasable

- The space must be ownable, currently unowned and the participant must have
  the complete configured price.
- Core commits movement, pauses at an immutable `PurchaseDecision` and stores a
  primitive continuation. No resource or ownership change occurs yet.
- Accept debits the exact price and assigns the participant once. Decline uses
  the registered `leave-unowned` policy and performs no economic mutation.
- Malformed, stale, duplicate, disallowed or wrong-participant responses are
  rejected without mutation.
- Ownership and resource notifications are emitted only after a valid accept.

Issue #35 owns further adversarial purchase-response hardening.

### UsageFee

- The space must have another participant as owner. Unowned and self-owned
  spaces do nothing.
- The configured fee is the obligation; the actual transfer is bounded by the
  payer's current balance. The payer stops at zero and the owner receives
  exactly the debited amount.
- Owner overflow rejects the prepared transition before either balance changes.
- Both committed balance changes are notified.

### Draw

- The referenced deck was validated before setup and is non-empty.
- The first current card is selected and rotated to the back in the committed
  transition.
- The card's effects run in declared order. A failed later effect also discards
  the prepared rotation and all earlier effect mutations.
- The committed draw emits `CardDrawnNotification` with generic card/deck IDs
  and profile presentation tokens.

## Card effects

### ResourceChange

- The resource must be declared by the validated profile.
- Positive deltas credit the exact amount. Overflow rejects the uncommitted
  transition.
- Negative deltas debit up to the available amount and never create a negative
  balance.
- Each committed change emits a resource notification.

### Move

- Relative offsets and absolute `SpaceId` targets use the validated track.
- `apply-profile-reward` applies the pass-origin reward for forward crossings;
  `ignore` never does.
- With `resolveDestination=false`, only position changes. With
  `resolveDestination=true`, the target uses the normal landing pipeline once.
- The baseline permits at most one movement effect per card and requires a
  destination-resolving move to be last. Nested draw destinations are rejected
  before setup until #36 defines cycle and chain behavior.

## Rounds and terminal scoring

The setup-selected starting participant is the round anchor. Turns follow the
original cyclic roster order. Returning to the anchor starts the next round.
After the last participant completes the configured final round, Core compares
the score resource and uses the registered lowest-player-ID tie break. The
winning state and match-ended notification commit together, after which the
notification source completes.
