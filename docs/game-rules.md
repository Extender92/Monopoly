# Property-trading capability baseline

## Scope

Rules are selected by validated profile declarations. Core does not contain
edition enums, regional presets, named deck assumptions or a fixed board size.

Schema version 1 supports the closed public vocabulary below. JSON cannot name
CLR types, callbacks, assemblies or scripts.

## Capabilities

Profile scope:

- move: enables circular movement on the ordered track.

Space scope:

- ownable: gives the space an owner and optional generic group ID.
- purchasable: assigns a non-negative price in a declared resource.
- usage-fee: assigns a fixed non-negative fee in a declared resource.
- draw: references a declared DeckId.

Purchasable and usage-fee capabilities require ownable. Every reference is
validated before the profile can be used.

## Card effects

- move: relative offset or absolute SpaceId, explicit pass-origin policy and
  explicit destination resolution.
- resource-change: a signed delta to one declared resource.
- status: apply or remove a declared generic status.

Effects run in declared order. Issue #75 defines the supported execution
semantics and rejects declarations outside that capability set.

## Setup registry

`GameSetup` recognizes the structural Demo baseline before a match is exposed:
the Move profile capability; Ownable, Purchasable, UsageFee and Draw space
capabilities; Move and ResourceChange effects; all three starting-player
policies; leave-unowned purchase decline; and the round-limited resource
result. Status definitions and effects are rejected until explicitly
supported.

Issue #75 attaches executable handlers to this same trusted registry. It does
not create a second capability vocabulary or accept arbitrary profile code.

## Lantern Vale Demo

The project-owned Demo is specified in
[Demo profile design](demo-profile-design.md) and stored as
[lantern-vale-v1.json](../profiles/demo/lantern-vale-v1.json).

It has 27 route spaces, one nine-card event deck, five independently designed
workshop groups and two resources. Its first playable baseline includes
movement, purchase, fixed usage fees, generic events and a round-limited
Renown score.

It intentionally excludes detention, auctions, inventory, building,
mortgaging, trading, held release cards and bankruptcy chains. Those behaviors
are not silently enabled by the engine.

Declining a Demo purchase leaves the space unowned. After round 12, the
participant with the highest Renown wins; the lowest player ID resolves a tie.

## Data ownership

The Demo has original names, text, layout, values and identifiers. Synthetic
tests use smaller structurally varied profiles. No complete third-party layout,
economic table or card collection is a test oracle or bundled profile.
