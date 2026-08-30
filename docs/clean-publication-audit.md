# Clean-publication audit

## Purpose

This document inventories the current public development tree for issue #55.
It separates file ownership from embedded content: a project-owned source file
can still contain presentation data that must be replaced before publication.

This is an engineering risk-control record, not legal approval. Private
ownership evidence belongs to #57. This document, the machine-readable
manifest and its verifier are audit/reference material for #58 and must not be
copied into the clean public repository.

## Publication policy

- Treat uncertain product-shaped presentation as blocked until it is replaced
  with original material or a documented right permits redistribution.
- Use original Demo data for product and integration flows and deliberately
  small synthetic profiles for focused capability tests.
- Do not publish a release, installer, deployment, advertisement or monetized
  build from this legacy repository.
- Do not migrate Git history, GitHub planning metadata or this raw audit into
  the clean repository.
- Run the publication verifier against both the proposed source snapshot and an
  unpacked Release publish directory before visibility changes.

## Baseline inventory

The baseline was collected from `main` before implementing #55.

| Surface | Observed baseline | Ownership | Classification | Disposition | Owner | Final snapshot |
| --- | --- | --- | --- | --- | --- | --- |
| Tracked tree | 136 files: 107 C#, 14 Markdown, 5 text/config dictionaries, 3 project files, 2 workflows and supporting solution/config files | Project-owned except identified dependencies and derived configuration | Review complete | Keep only after all content rules pass | #55 | Reviewed files only |
| Core board/card data | Regional board names, values, layouts and verbatim cards in `Monopoly.Core/Data/Data.cs` | Project code with third-party presentation | Replace | Generic profile data and original Demo content | #4 | No current dataset |
| Regional card implementations | Four UK/US card classes and regional effect enum values | Project code with product-shaped identifiers and behavior coupling | Replace | Generic card effects, stable IDs and profile policy | #4, #37 | Generic implementation only |
| Console presentation | Named decks, squares, detention presentation and legacy product identity | Project-owned | Replace | Consume profile metadata and neutral identity | #4, #56 | Neutral presentation only |
| Tests | Exact board names, current deck/card types, regional constructors and product-shaped fixture values | Project-owned | Replace | Small synthetic capability fixtures and original Demo integration data | #4, #37 | Demo/synthetic tests only |
| Documentation | Official edition descriptions, publication codes, source URLs, exact layouts/economies and legacy repository identity | Project-owned prose containing third-party references | Remove/replace | Generic capability documentation and neutral project identity | #4, #37, #56 | Reviewed neutral docs only |
| Save contract | `GameLanguage`, regional deck fields, fixed board size and legacy presentation-shaped state | Project-owned | Replace | Generic profile/deck/card/square IDs in Version 2 | #52 | Version 2 only |
| Project identity | Legacy repository, solution, project, assembly, namespace, command, badge and documentation names occur throughout the tree | Project-owned | Replace | Apply the neutral identity selected by #56 | #56 | Neutral identity only |
| Assets | No tracked image, audio, font, archive or other binary presentation asset | Not applicable | Allow current absence | Any future binary requires explicit classification | #55 | Classified assets only |
| Saves | Default `game_data.json` was possible at repository root and was not ignored | User-generated match data | Remove | Ignore local saves; never export them | #55, #58 | Excluded |
| Build/test output | `.vs`, `bin` and `obj` directories exist only as ignored local output; no ignored file is tracked | Generated | Remove | Exclude build, test and temporary output from source snapshot | #58 | Excluded |
| GitHub delivery | Public fork with no GitHub Releases, Pages site or deployments at audit time | Repository metadata | Review | Keep development-only; do not create delivery artifacts | #55, #58 | Not migrated |
| Git metadata | Legacy tags `v0.1.0` and `v0.1.1` exist | Historical metadata | Remove from cutover | New repository starts with one clean root commit | #58 | Excluded |
| Audit material | This document and `eng/publication/` contain old names, evidence and deny patterns | Project audit record | Remove from cutover | Use externally during #58 and retain only in the encrypted archive | #58, #60 | Excluded |

## Dependency inventory

The entries below record the resolved dependency surface. #57 must verify each
license against authoritative upstream evidence, decide compatibility and
prepare any required notice before the publication gate can pass.

### NuGet and platform dependencies

| Dependency | Resolved version | Kind | Current status | Owner |
| --- | --- | --- | --- | --- |
| Microsoft.NET.Test.Sdk | 18.9.0 | Direct test dependency | License/notice review required | #57 |
| Moq | 4.20.72 | Direct test dependency | License/notice review required | #57 |
| xunit | 2.9.3 | Direct test dependency | License/notice review required | #57 |
| xunit.runner.visualstudio | 4.0.0 | Direct test dependency | License/notice review required | #57 |
| coverlet.collector | 10.0.1 | Direct test dependency | License/notice review required | #57 |
| Castle.Core | 5.1.1 | Transitive test dependency | License/notice review required | #57 |
| Microsoft.CodeCoverage | 18.9.0 | Transitive test dependency | License/notice review required | #57 |
| Microsoft.TestPlatform.ObjectModel | 18.9.0 | Transitive test dependency | License/notice review required | #57 |
| Microsoft.TestPlatform.TestHost | 18.9.0 | Transitive test dependency | License/notice review required | #57 |
| System.Diagnostics.EventLog | 6.0.0 | Transitive test dependency | License/notice review required | #57 |
| xunit.abstractions | 2.0.3 | Transitive test dependency | License/notice review required | #57 |
| xunit.analyzers | 1.18.0 | Transitive test dependency | License/notice review required | #57 |
| xunit.assert | 2.9.3 | Transitive test dependency | License/notice review required | #57 |
| xunit.core | 2.9.3 | Transitive test dependency | License/notice review required | #57 |
| xunit.extensibility.core | 2.9.3 | Transitive test dependency | License/notice review required | #57 |
| xunit.extensibility.execution | 2.9.3 | Transitive test dependency | License/notice review required | #57 |
| .NET SDK | 10.0.201 selected by `global.json` | Build platform | Confirm redistribution boundary for final artifacts | #57, #58 |

### Workflow dependencies

| Dependency | Current reference | Current status | Owner |
| --- | --- | --- | --- |
| actions/checkout | `v4` | License/source and immutable-reference review required | #53, #57 |
| actions/setup-dotnet | `v6` | License/source and immutable-reference review required | #53, #57 |
| actions/upload-artifact | `v7` | License/source and immutable-reference review required | #53, #57 |
| check-spelling/check-spelling | `v0.0.26`, `main` and `prerelease` | Derived configuration plus license/source and mutable-reference review required | #53, #57 |
| cspell software terms dictionary | Remote workflow input | License/source review required | #57 |

## Denylist categories

The machine manifest contains the exact literals and patterns. These categories
are blocked from the clean snapshot unless the manifest is deliberately updated
with documented evidence and review:

- Legacy product, repository, solution, assembly, namespace and package identity.
- Publisher names, publication codes, official source URLs and attribution that
  does not belong in the neutral product.
- UK/US profile names, regional type names and edition-selection symbols.
- Official square, station, railroad, utility and tax names.
- Verbatim Chance and Community Chest text and product-specific deck names.
- Complete official layout, economy, inventory or card-set reproductions,
  including relabelled copies.
- Legacy save fields and presentation-shaped identifiers.
- Unknown binaries, unreviewed dependencies and unresolved content findings.
- Git metadata, saves, build/test output, temporary files and audit/reference
  material.

## Allowlist policy

The allowlist is narrow and path-scoped. It permits independently written
property-trading mechanics, generic concepts such as dice, movement, ownership,
rent, auctions, detention, mortgages, building, trading and bankruptcy, and the
project's own implementation when no deny rule also matches. An allow entry
does not override a deny rule merely because both occur in the same file.

Tool-specific spelling dictionaries are not part of the publication allowlist.
For example, an entry in `.github/actions/spelling/allow.txt` only suppresses a
spelling warning; the publication verifier still scans that file, classifies
denylisted content and blocks it in Publication mode.

Original Demo presentation is allowed after the #4 review recorded below.
Dependency code or notices do not become allowed until #57 records authoritative
license evidence and the required notice decision.

## Issue #4 outcome

Issue #4 removed the tracked regional board/card dataset, regional card
classes, edition selection, regional rule documents and Version 1 DTO/fixture.
The replacement is the project-owned Lantern Vale JSON profile, validated by
the schema-version 1 parser and locked to its reviewed revision fingerprint.

Issue #75 removed the product-shaped Core executor, concrete legacy runtime
types and their regression fixtures. The supported runtime now executes only
validated Demo/synthetic capabilities through one registry. Product-shaped
Console session sources remain tracked but excluded from compilation and are
owned by #77. Version 1 appears only in compatibility detection and audit
evidence owned by #52. Final artifact and snapshot confirmation remains #58's
responsibility.

## Verification contract

Audit mode proves that every tracked file and every known sensitive occurrence
has a classification and an owning issue. Known replacement work is reported
but does not fail the audit.

Publication mode is the clean-cutover gate. It rejects every `replace`,
`remove`, `review`, unknown or unowned finding, forbidden path, unknown binary,
unapproved dependency and missing publication file. It scans both the source
snapshot and a separate unpacked Release publish directory.

Run from the repository root:

```powershell
pwsh eng/publication/verify-clean-publication.ps1 `
  -Mode Audit `
  -Root . `
  -ReportPath ../publication-audit.json
```

At #58, run the raw verifier from outside the proposed clean snapshot:

```powershell
pwsh eng/publication/verify-clean-publication.ps1 `
  -Mode Publication `
  -Root <clean-snapshot> `
  -ArtifactRoot <unpacked-release-publish> `
  -ReportPath <private-path-outside-snapshot>/publication-audit.json
```

Exit code `0` means the selected gate passed, `1` means policy violations were
found and `2` means the manifest, input or verifier execution was invalid.

## Completion handoff

- #4 and #37 consume the board, card, test-data and capability findings.
- #75 removed the legacy Core runtime; #77 removes the temporarily excluded
  Console compatibility sources and supplies generic projections.
- #52 replaces legacy persistence identifiers and fields.
- #56 consumes the identity and URL findings.
- #57 resolves every dependency and notice row without publishing private
  ownership evidence.
- #58 reruns Publication mode against source and artifacts, excludes this raw
  audit and changes visibility only after a clean result.
- #60 retains this audit only inside the encrypted archive before legacy
  repository deletion.
