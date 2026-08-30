# JSON profile format

## Purpose

Game-profile JSON is an untrusted, declarative input format. It describes one
property-trading profile without supplying code, callbacks, CLR types, file
paths or assemblies. A profile cannot create a match until Infrastructure has
parsed the document and Core has returned an immutable `ValidatedGameProfile`.

The tracked schema for format version 1 is
[`game-profile-v1.schema.json`](../profiles/schema/game-profile-v1.schema.json).
The schema is the authoring contract; Core semantic validation remains
authoritative for relationships that JSON Schema cannot express.

## Ownership and data flow

```text
UTF-8 JSON bytes or bounded stream
        |
Infrastructure strict JSON parser
        |
transport-neutral Core definitions from #72 and #73
        |
Core semantic validator and canonical fingerprint
        |
immutable ValidatedGameProfile
```

Infrastructure owns `System.Text.Json`, wire DTOs and technical format errors.
Core owns identifiers, capabilities, effects, policies, semantic validation and
fingerprinting. Core never receives a path. File selection and loading belong
to the application boundary introduced by issue #76.

## Version 1 structure

A version 1 document contains:

- `schemaVersion`, `profileId` and positive `revision`;
- one profile presentation token and a presentation catalog;
- declared resources and their presentation tokens;
- setup limits, dice, start space, starting resources and starting-player
  policy;
- one ordered track and exactly one definition for every space;
- zero or more decks containing globally unique card IDs;
- profile and space capabilities, ordered card effects and statuses; and
- pass-origin, purchase-decline and round-limited match-end policies.

The schema uses camel-case property names and rejects unknown properties.
Identifier and presentation-token values contain at most 128 characters and
use lowercase ASCII segments separated by `.` or `-`.

The supported starting-player values are `fixed-order`, `random` and
`highest-roll`. The version 1 public baseline declares `leave-unowned` for a
declined purchase and `highest-resource-after-rounds` with
`lowest-player-id` as its deterministic tie-break. These values are data;
issues #40 and #75 own setup and execution.

Capabilities use an explicit `kind`:

- `move` at profile scope;
- `ownable`, optionally with a group ID;
- `purchasable`, with a non-negative resource amount;
- `usage-fee`, with a non-negative resource amount; and
- `draw`, with a deck ID.

Effects also use an explicit `kind`:

- `move`, with either a non-zero relative offset or absolute space ID;
- `resource-change`, with a signed non-zero delta; and
- `status`, with an apply/remove operation and validated value.

Unknown kinds and fields are rejected. Version 1 has no extension bag or
fallback interpretation.

## Validation and limits

The parser accepts UTF-8 with an optional byte-order mark. It is
case-sensitive and rejects UTF-16, invalid UTF-8, comments, trailing commas,
duplicate members and values with the wrong JSON type.

The following limits apply before a profile can be returned:

| Input | Maximum |
| --- | ---: |
| UTF-8 document | 5 MiB |
| JSON depth | 64 |
| Track spaces and space definitions | 512 |
| Decks | 32 |
| Cards across all decks | 2,048 |
| Effects on one card | 32 |
| Unicode scalar values in one presentation text | 4,096 |

Core additionally requires unique identifiers, globally unique card IDs,
complete presentation references, a valid start space, one starting amount for
every resource and valid space, deck, card, resource and status references.
`Purchasable` and `UsageFee` require `Ownable`, and only `move` is valid at
profile scope.

Infrastructure reports technical failures as `ProfileJsonException` with a
`ProfileJsonErrorKind` and path. Core reports semantic failures as
`ProfileValidationException` with a `ProfileValidationErrorKind` and path.
Failure returns no profile and cannot create or mutate a `Game`.

## Canonical fingerprint

The JSON document does not contain its own fingerprint. After semantic
validation, Core writes a version-marked canonical representation and hashes it
with SHA-256. The resulting `ProfileFingerprint` is 64 lowercase hexadecimal
characters.

Canonicalization sorts catalogs that have no rule-significant order by their
stable IDs. It preserves track order, card order and effect order. JSON
whitespace, a UTF-8 byte-order mark, property order and input ordering of
unordered catalogs do not change the fingerprint. Any semantic change to
identity, revision, presentation, setup, structure, rules, cards, effects,
statuses or policies does change it.

Save Format Version 2 records profile ID, revision and this fingerprint. It can
therefore reject a missing or changed profile before reconstructing a match.
Version 1 saves remain unchanged and contain none of the profile JSON.

## Authoring and testing

Profile authors should validate against the tracked schema and then use the
runtime parser to perform semantic validation. Schema validation alone cannot
prove that referenced IDs exist or that capability combinations are legal.

The repository contains small original schema-conformance fixtures with
different track and deck structures. The bundled original Demo profile is
introduced separately by issue #4. Builds, tests and default application use
must never depend on an external or private profile.

## Related documentation

- [Public engine scope](public-engine-scope.md)
- [Original Demo design](demo-profile-design.md)
- [Architecture](architecture.md)
- [Game flow](game-flow.md)
- [Save and load](save-format.md)
- [Testing](testing.md)
