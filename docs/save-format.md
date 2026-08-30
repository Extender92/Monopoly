# Save and load

## Current state

There is an intentional persistence gap.

Save Format Version 1, its DTOs, mapper and fixture have been removed because
they represented retired profile fields and could not identify the validated
profile that defines a match.

JsonFileGameSaveStore remains the injected Infrastructure adapter so Console
composition does not bypass the storage boundary. During the gap:

- Save throws SaveStoreException with IncompatibleVersion before any file
  operation;
- Load recognizes a Version 1 envelope and rejects it with the same typed
  compatibility category;
- unsupported or absent versions are compatibility errors;
- malformed JSON remains InvalidData;
- missing files remain NotFound;
- technical access failures remain StorageFailure.

A failed operation does not create, replace or mutate a save file or active
match.

The runtime records the exact validated profile identity, revision and
fingerprint, resource balances and SpaceIds, deck order, ownership, round,
winner, phase and primitive purchase continuation. These are authoritative
runtime/read-model contracts, not a temporary wire format; persistence remains
unavailable until Version 2 can validate and reconstruct the whole match.

## Version 2 target

Issue #52 owns the replacement. Version 2 must include:

- profile ID, revision and canonical SHA-256 fingerprint;
- generic resource balances, space IDs, deck IDs and card IDs;
- current deck order;
- player position, ownership and supported module state;
- match phase, pending decision and continuation state;
- winner and game-over state.

Loading requires the exact referenced profile to be registered. A missing or
changed profile is rejected before an active match is replaced. The complete
candidate is reconstructed and validated as one unit.

Runtime services such as random-source state, subscribers and frontend input
are never serialized.

## Ownership boundaries

Core owns persistence contracts, compatibility categories and whole-match
validation. Infrastructure owns JSON encoding, paths and atomic physical file
operations. Console owns save selection and user-facing error presentation.

Issue #52 does not restore compatibility with retired files.
