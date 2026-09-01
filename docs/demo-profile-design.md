# Original Demo profile design

## Purpose

The Demo is the self-contained reference product for the public engine. It must
prove the supported capabilities without recreating the structure,
presentation, wording or economic table of an existing commercial game.

The working theme is **Lantern Vale**, a fictional cooperative of small makers
leasing workshop sites along a circular delivery route. The theme and working
name remain subject to the neutral-name and independent-review gates in issues
#56 and #79.

## Structural design constraints

The schema-version 1 Demo delivered by issue #4 satisfies these constraints:

- 27 ordered spaces, with visual sides treated only as optional presentation
  metadata;
- no four equivalent corner or special-space pattern;
- one event deck containing nine originally written event definitions;
- opaque, non-regional profile, space, deck and card identifiers;
- a mix of neutral route spaces and purchasable workshop sites;
- fixed profile-defined usage fees rather than a copied rent table;
- one single-source movement mechanism selected by the profile;
- a twelve-round limit followed by profile-defined score evaluation; and
- values and group sizes designed for this Demo and tested for internal balance.

The reviewed data is stored in
[`profiles/demo/lantern-vale-v1.json`](../profiles/demo/lantern-vale-v1.json).
Revision 1 has canonical fingerprint
`7ba140a86da1a20222f2580b7419ca7e3f52d7a392bcadf9269ed1fe5a456c7d`.
A semantic change requires a new revision and an intentionally updated locked
fingerprint.

## First capability baseline

The Demo activates only the public baseline owned by issue #75:

- `Move` for ordered-route movement;
- `Ownable` and `Purchasable` for workshop sites;
- `UsageFee` with a fixed configured amount;
- `Draw` from the single event deck;
- `ResourceChange` for bounded event outcomes; and
- the profile-defined round and score policy.

A declined purchase leaves the site unowned and continues under Demo policy.
The first Demo does not require an auction or an implicit compatibility flow.

The first version does not activate detention, mortgage, building, trading,
held-release-card, bankruptcy-chain or other deferred capability modules. An
unknown capability is a validation failure rather than a silent no-op.

## Originality requirements

Every Demo name, description, event text, identifier, value, order, group,
policy and presentation token must be written for this project. The complete
dataset must not be an existing edition with renamed labels or proportionally
rescaled values.

The Demo must not reproduce:

- an established full route order or economic progression;
- a fixed product-shaped set of special spaces;
- an established pair of deck roles or complete card list;
- official names, characters, logos, graphics, wording or visual trade dress;
  or
- compatibility identifiers intended to load third-party data.

Small synthetic test profiles must be structurally different from the Demo and
from each other. Focused tests assert contracts and invariants instead of a
complete product dataset.

## Validation and evidence

Issue #4 delivered the final JSON and content replacement. The reusable
structural contracts and strict schema validator are defined in
[profile-format.md](profile-format.md). The deterministic Demo scenario parses
that JSON and proves setup, every supported capability, persistable state,
notification order, 12-round scoring and one terminal winner. Issue #78 checks
the wider structural variation and publication leakage controls.

Before publication, the Demo source and rendered/build artifacts must pass the
issue #55 manifest, dependency/license review in #57 and the independent review
gate in #79. Passing those gates records due diligence and risk reduction; it
does not turn the Demo into a legal opinion.
