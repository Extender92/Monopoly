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

- The space must be ownable and currently unowned. Price and resource identity
  come from the validated capability.
- Core commits movement, pauses at an immutable `PurchaseDecision` and stores a
  primitive continuation. No resource or ownership change occurs yet.
- A participant who cannot afford the complete price receives no decision. The
  same registered non-purchase policy used by an explicit decline runs instead.
- A response validates the phase, opaque decision ID, participant,
  continuation, current space, capability, price, ownership and affordability.
- Accept uses an exact debit and assigns the participant in one detached
  transition. Decline uses the registered `leave-unowned` policy and performs
  no economic mutation.
- A policy returns either `Continue` or a request for a capability independently
  registered in the trusted Core registry. Schema version 1 only produces
  `Continue`; it contains no auction policy or capability.
- Malformed, stale, duplicate, disallowed, wrong-participant, insufficient-
  resource and changed-precondition responses are rejected without mutation.
- Resource, ownership and generic decision-resolution notifications are emitted
  only after the complete response transition commits.

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
- A destination may resolve another Draw capability. If it references the same
  deck, that nested draw observes the already prepared rotation and therefore
  selects the next current card.
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
- A card may contain multiple non-resolving moves mixed with resource changes.
  It may contain at most one destination-resolving move, which must be its final
  effect. This terminal rule means no suspended outer effect work remains when
  a destination pauses for a decision.

## Effect chains and decisions

Setup simulates every card from every space that can draw its deck. Terminal
resolving moves form a graph between Draw spaces. Any possible self-loop or
longer cycle is rejected independently of initial deck order; valid chains are
therefore finite and deterministic.

Each requested destination runs the same ordered landing pipeline exactly
once. A purchase may pause that pipeline. After its response, the primitive
continuation resumes at the next capability and may reach another purchase
decision through a later nested draw. The new decision replaces the completed
boundary while retaining the original turn roll. Earlier draws, effects and
landings are not replayed.

Core derives the completed `TurnResult` from the actor's committed SpaceId.
Notifications for each action segment remain staged until that segment commits;
a failed chain publishes none of its prepared notifications.

## Rounds and terminal scoring

The setup-selected starting participant is the round anchor. Turns follow the
original cyclic roster order. Returning to the anchor starts the next round.
After the last participant completes the configured final round, Core compares
the score resource and uses the registered lowest-player-ID tie break. The
winning state and match-ended notification commit together, after which the
notification source completes.
