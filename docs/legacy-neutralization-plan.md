# Legacy neutralization boundary

## Status and disposition

This is legacy audit/reference material for the current work-in-progress
repository. Issue #58 must not export it to the clean publication snapshot.
It records why current files can remain temporarily while focused replacement
issues are implemented.

## Current-state categories to remove or replace

The legacy tree still contains product-shaped material in several independent
categories:

| Category | Required disposition | Owner |
| --- | --- | --- |
| Regional board, card and destination data | Replace with original Demo JSON and generic IDs | #4 |
| Fixed board/deck and concrete square/card/status types | Replace with structural and capability contracts | #72, #73 |
| Profile parsing and validation assumptions | Replace with bounded schema v1 | #74 |
| Product-shaped rule and setup behavior | Restrict to the supported public baseline | #40, #75 |
| Frontend-specific presentation values in Core | Semantic profile metadata boundary implemented; final content/projections remain separate gates | #4, #77 |
| Regional Version 1 save fields | Remove and replace with exact-profile Save V2 | #52 |
| Legacy solution, package, namespace, command and URL identity | Replace before clean export | #56 |
| Legacy Console type branches | Replace with generic projections | #77 |
| Legacy documentation and tests | Replace with public scope, Demo and synthetic fixtures | #4, #54, #78 |

The detailed denylist and per-file inventory remain in
`docs/clean-publication-audit.md` and `eng/publication/`. Those files, including
this document, are development evidence and not public product documentation.

## Local profile boundary

A developer may keep optional mechanics-only profiles outside every repository.
They are untrusted local inputs using the same public JSON schema. They are not
an implementation specification, test oracle, CI input, backup of public data
or publication candidate.

Tracked files must not contain a developer-specific external path. No local
profile is copied into source, logs, saves, test fixtures, publish output or the
clean root. The repository defensively ignores local profile conventions, but
issue #78 and the publication verifier must still reject leakage.

## Completion gates

Neutralization is complete only when:

1. the positive public documents describe the intended engine and Demo;
2. the generic structure, capability and JSON contracts replace legacy types;
3. original Demo and synthetic data replace product-shaped content;
4. Save V2 and Console consume generic identifiers and projections;
5. source and Release artifacts pass the publication manifest;
6. the content-identical clean candidate passes independent review; and
7. issue #58 exports only its approved allowlisted snapshot.

Historical Git content is not rewritten because no legacy history is imported
into the clean repository. This operational choice does not make current
legacy material suitable for redistribution and is not legal advice.
