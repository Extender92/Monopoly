# Testing

## Purpose

This document defines the test strategy and verification requirements for the
Monopoly repository.

The primary goal is confidence that one authoritative Core can run complete UK
Classic, US Classic and Custom matches for any supported frontend. Tests must
protect game behavior, architectural boundaries, persistence compatibility and
frontend integration without making implementation refactoring unnecessarily
difficult.

The target strategy is normative. The later current implementation section is
informational and describes the transitional test suite that exists today.

## Quality goals

The test system must provide:

- Fast feedback for isolated rule and calculation changes.
- Deterministic verification of complete Core state transitions.
- Shared rule-profile contracts for UK Classic, US Classic and Custom.
- Regression protection for every corrected defect.
- Persistence compatibility and invalid-state protection.
- Frontend tests that do not duplicate Monopoly rules.
- Infrastructure tests that do not depend on a user's real files or services.
- CI verification equivalent to the required local Release build and test run.
- Clear failures that identify the broken behavior and expected invariant.

Passing tests are necessary but do not replace review of the final diff,
warnings, architecture or manual frontend behavior.

## Test boundaries

Tests should mirror production responsibilities.

```text
Core tests
    Rules, state, commands, turn flow and persistence contracts

Infrastructure tests
    Serialization, files and other technical storage implementations

Console tests
    Input, navigation, decision mapping and rendering

End-to-end scenarios
    Composition of real boundaries around deterministic match behavior
```

A Console test must not become the only test for a Monopoly rule. A file-storage
test must not be required to prove a Core state transition. Core tests must not
depend on an interactive terminal.

The repository may keep these boundaries as folders in one test project while
the solution is small. Separate test projects should be introduced when a
production project such as Infrastructure needs independent dependencies,
fixtures or execution behavior.

## Core unit tests

Core unit tests cover isolated calculations, value objects, validators and
small domain operations.

Appropriate subjects include:

- Rule-profile validation and resolution.
- Rent and mortgage calculations.
- Auction bid validation.
- Building eligibility and inventory calculations.
- Trade-offer validation.
- Card instruction calculations.
- Stable identity and reference rules.
- Board coordinate-independent movement calculations.
- Money, debt and asset-value calculations.

Unit tests should:

- Construct only the minimum required state.
- Avoid Console and physical storage.
- Avoid production randomness; inject a scripted match source.
- Assert domain results and rejected invalid input.
- Prefer public behavior over private implementation details.
- Remain valid when internal class construction is refactored.

Mocks are useful for true external collaborators. They should not be used merely
to call and verify the same mocked method, because that tests the mocking
framework rather than production behavior.

## Core integration tests

Core integration tests exercise real `Game`, board, players, transactions,
cards and rules together through public Core operations.

They are required when a change affects:

- Setup or starting-player selection.
- Turn phases or player rotation.
- Movement, passing GO or landing chains.
- Doubles or Jail.
- Purchases, auctions or rent claims.
- Payments and insufficient funds.
- Cards that move players or create debts.
- Mortgages, buildings or finite bank inventory.
- Trades and ownership transfer.
- Bankruptcy or winner state.
- Pending decisions or resumable match phases.

An integration test should normally:

1. Resolve an explicit rule profile.
2. Build a match with stable player IDs.
3. Inject a deterministic `IMatchRandomSource` and scripted decisions.
4. Arrange only state that could legally exist, unless invalid-state rejection
   is the subject.
5. Invoke the public command or turn entry point.
6. Assert the returned result.
7. Assert all affected authoritative state.
8. Assert that unrelated state did not change.

For example, a rent test should verify the debtor balance, creditor balance,
ownership, bankruptcy state, current player and actual amount sent to any
decision boundary.

## Required game-flow coverage

The completed Core suite must cover at least the following behavior groups.

### Setup

- Supported and rejected player counts.
- Profile-defined starting money.
- Stable and unique player IDs.
- Starting-player rolls and tied highest rolls.
- Setup rolls not affecting movement, turns or doubles.

### Movement and turn progression

- Normal movement.
- Passing GO and landing exactly on GO.
- Card-driven forward and backward movement.
- Chained landing effects.
- Normal player rotation.
- Extra rolls after eligible doubles.
- Third consecutive doubles sending the player directly to Jail.
- No skipped or duplicated players after removal.

### Jail

- Entering Jail from every supported cause.
- Paying before rolling.
- Selecting and using a specific held card.
- Remaining in Jail to attempt doubles.
- Doubles release, movement and landing without an extra roll.
- Forced release after the profile limit.
- Insufficient funds and bankruptcy during a forced payment.
- Save/load and continuation for every stable Jail phase.

### Property, rent and auctions

- Purchase confirmation before payment resolution.
- Exact cash being sufficient.
- Declined or unaffordable properties entering the correct auction.
- Auction participation, withdrawal, minimum increases and winner.
- Rent claims before the configured expiry boundary.
- No rent for self-owned or mortgaged property.
- Correct Street, Railroad and Utility rent.
- Dedicated Utility rent dice not affecting turn doubles or movement.

### Money and debt

- Exact-balance rent, tax, fine and card payments.
- The actual debt amount reaching insufficient-funds handling.
- Asset sales and mortgages making progress.
- No-progress handling terminating safely.
- Bank debts and player-creditor debts remaining distinct.
- Multi-player debts using the documented active-player creditor order.

### Buildings, mortgages and trades

- Complete-set and even-building rules.
- House and Hotel purchase, sale and exchange.
- Finite bank inventory and shortage auctions.
- Mortgage and unmortgage eligibility and amounts.
- Mortgage handling after ownership transfer.
- Accepted, declined and invalid trades.
- Atomic trade application.
- Valid transaction windows, including while in Jail.

### Cards

- Every card instruction for each supported Classic profile.
- Correct final landing after card movement.
- Stable card identity and source deck.
- Held Get Out of Jail Free cards leaving their deck.
- Use, trade, bankruptcy transfer and return to the correct deck.
- Repair fees and multi-player payment cards.

### Bankruptcy and completion

- Bankruptcy to another player.
- Bankruptcy to the bank.
- Money, property, mortgage, buildings and cards handled exactly once.
- Bank auctions after bank-creditor bankruptcy.
- Turn rotation advancing at most once.
- Immediate winner assignment when one active player remains.
- Further turn commands rejected or reported as complete after game over.

These lists define behavior groups, not a requirement for one oversized test.
Use focused scenarios with clear failure reasons.

## Rule-profile contract tests

Shared contract tests must run against:

- UK Classic.
- US Classic.
- Representative valid Custom profiles.

UK and US contract coverage should verify:

- Profile identity and revision.
- Supported player limits.
- Starting money and dice.
- Salary, Jail fine and mortgage interest.
- Free Parking behavior.
- Board size, square identity and regional names.
- Property groups, prices, rent and building costs.
- Chance and Community Chest identity and instructions.
- Auction and rent-claim rules.
- House and Hotel bank inventory.
- Trade and bankruptcy behavior.

The same behavior should use parameterized tests where profiles share a rule.
Regional values should be asserted explicitly where they differ.

Custom tests should include:

- A valid profile with representative overrides.
- Minimum and maximum accepted values.
- Unsupported combinations.
- Missing required data.
- Invalid board, card, economy and dice configuration.
- A resolved profile remaining unchanged for the life of a match.

Tests should use the selected canonical edition data documented under
[`rules/`](rules/), not values independently copied from a frontend.

## Save/load contract tests

Core persistence-contract tests verify logical match state without requiring a
physical file.

Every supported schema version needs coverage for:

- A complete round trip.
- UK Classic and US Classic.
- Representative Custom rules.
- Current player and player order.
- Ownership, mortgage and buildings.
- Bank inventory and fine pot.
- Jail state and turns.
- Stable card identity, held cards and exact deck order.
- Turn phase, doubles, winner and game-over state.
- Every supported pending decision type.
- Reconstruction of all references to the same player instances.

Invalid-state tests must cover:

- Missing or unsupported versions.
- Missing required sections.
- Duplicate or unknown IDs.
- Invalid positions, ranges and collection lengths.
- Unknown profile or profile revision.
- Illegal ownership, mortgage and building combinations.
- Missing or duplicated cards.
- Inconsistent bank inventory.
- Impossible turn, bankruptcy or pending-decision state.

A failed load must not mutate or replace the caller's active match.

Serialization and physical storage behavior belong to Infrastructure tests. The
full persistence contract is documented in
[save-format.md](save-format.md).

## Infrastructure tests

Infrastructure tests cover the current `JsonFileGameSaveStore` implementation.

They should cover:

- Serialization and deserialization for each supported DTO version.
- Stable expected property names and enum representation.
- File naming and configured save directories.
- Listing and selecting saves.
- Missing and inaccessible files.
- Malformed serialized content.
- Safe replacement of an existing save.
- Failure during writing without a partially valid destination.
- Concurrent access behavior where supported.

File tests must use a unique temporary directory owned by that test. Cleanup
must target only that verified directory and run even when an assertion fails.
Tests must never read, overwrite or delete a developer's real save.

Database, browser or cloud implementations require their own boundary tests.
External integration tests must be explicitly configured and must not make the
default local suite depend on unavailable accounts or networks.

## Console tests

Console tests verify presentation and interaction, not Monopoly rules.

They should cover:

- Arrow-key navigation, Enter and valid Escape cancellation.
- Menu transitions without recursive session loops.
- Setup input mapped to a rule-profile request.
- Core-provided choices rendered and returned correctly.
- Decision prompts using current profile values.
- Board, card, player, building, mortgage and winner rendering.
- Semantic color tokens mapped locally to `ConsoleColor` with white fallback.
- Unknown or missing layout hints retaining position-based rendering.
- Visible text falling back to the stable token when labels are absent.
- Save selection and error presentation.
- One refresh per logical change without duplicate log output.
- Match-scoped subscription and cleanup.
- Shared composition behavior for new and loaded games.
- Terminal operations routed through `IConsoleWrapper`.

Use a fake or mock `IConsoleWrapper` to record output and supply input. Do not
require a real interactive terminal in the automated suite.

Core commands should be faked or driven through a prepared real game depending
on whether the test concerns UI mapping or integrated frontend composition.
The Console assertion remains about what was shown or submitted, not whether a
Monopoly rule was calculated correctly.

The Console boundary is documented in
[console-frontend.md](console-frontend.md).

## End-to-end match scenarios

At least one deterministic automated scenario should compose the real Core with
scripted dice and decisions and play from valid setup to a winner.

The scenario should include representative transitions such as:

- Starting-player selection.
- Normal and doubles turns.
- A purchase and an auction.
- Rent and insufficient-funds handling.
- Jail entry and release.
- Card movement.
- Mortgage or building management.
- Bankruptcy and winner assignment.

This is not a substitute for focused tests. Its purpose is to prove that the
public operations compose into a complete match without deadlock, duplicate
turn progression or unhandled pending state.

Interactive Console smoke testing remains separate because terminal rendering
cannot provide deterministic rule coverage.

## Determinism and test doubles

Automated tests must control all nondeterministic inputs that affect assertions.

Use:

- `ScriptedMatchRandomSource` with one queued sequence for the whole match.
- Assertions on `RandomPurpose`, ranges and sequence indices when the caller
  being tested owns the request.
- Scripted player decisions.
- Stable player, square and card IDs.
- Explicit rule profiles.
- A controlled clock if time limits are introduced.
- Unique temporary storage paths.

Do not rely on:

- Real random rolls.
- Current time or locale defaults.
- Test execution order.
- Existing files on the machine.
- Interactive input.
- Network services in the default suite.
- Arbitrary sleeps.

A scripted random source reports exhaustion explicitly rather than repeating a
previous value. Tests for a multi-value operation must also prove that failure
before the final value leaves all authoritative state, pending decisions, logs
and notifications unchanged. A minimum-value source is suitable only where a
test needs deterministic setup but does not assert a particular shuffled order.

A scripted decision provider should record every request and submitted amount so
tests can assert that Core asked the correct question exactly once.

Shared builders and scenario helpers are encouraged when they keep legal setup
concise. They must not hide the behavior under test or silently repair invalid
arrangements.

## Isolation, cleanup and parallel execution

Each test owns its match, test doubles, subscriptions and temporary resources.
One test must not affect another.

- Dispose every match notification subscription in `finally`, with a `using`
  declaration or through a disposable fixture.
- Remove only test-owned temporary files and directories.
- Restore modified process-wide Console state.
- Avoid mutable static test fixtures.
- Avoid assertions that depend on another test running first.
- Use unique IDs and paths when tests can run concurrently.

Tests that temporarily interact with unavoidable process-global state must be
placed in an explicit non-parallel collection. Match notification tests do not
need that exception: every `Game` has an isolated source and no process-global
subscriber list. Disabling parallel execution for the entire suite should not
be the permanent solution.

Match-scoped Core notifications and frontend sessions allow independent game
tests to run safely in parallel.

## Regression tests

Every corrected defect requires a test that fails for the original behavior and
passes after the correction.

A regression test should:

- Reproduce the smallest realistic triggering flow.
- Use the public boundary where the defect was observable.
- Assert the incorrect outcome cannot recur.
- Include related state consistency, not only the thrown exception.
- Use the relevant rule profiles.

For a Console crash caused by Jail state, for example, tests should cover both
the Core Jail transition and the Console flow that previously requested stale
state.

Do not remove or weaken a regression test merely to accommodate a refactor.
Update it to use the new public contract while preserving the protected
behavior.

## Naming and organization

Test names should describe behavior clearly. A useful pattern is:

```text
Operation_WhenCondition_ProducesExpectedResult
```

Natural behavior-focused names are also acceptable when they remain precise.
Avoid names that only repeat a method name or say that an object "works".

Organize tests by production boundary and behavior:

```text
CoreTests/
    Unit/
    Integration/
    RuleProfiles/
    Persistence/

ConsoleTests/
    Input/
    Navigation/
    Rendering/
    Composition/

InfrastructureTests/
    Serialization/
    FileStorage/
```

The exact folder depth may remain smaller while the suite is small. Empty test
classes should be removed or populated with meaningful behavior tests.

Use Arrange, Act and Assert separation where it improves readability. Prefer one
clear behavioral reason for failure, while asserting all state that belongs to
that transition.

## Local verification

From the repository root, the standard full verification is:

```text
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release --no-build
```

The SDK selected by `global.json` must be installed.

During development, a focused filter may be used for faster feedback:

```text
dotnet test Monopoly.Tests --filter FullyQualifiedName~GameFlowIntegrationTests
```

A focused run never replaces the full suite before handoff, commit, pull request
or merge.

The final result must report:

- Passed, failed and skipped tests.
- Build warnings and errors.
- Any intentionally excluded external or manual checks.

No new warning, failure or unexplained skipped test is accepted.

## Continuous integration

CI must run for pushes and pull requests and use the repository's supported
stable .NET SDK.

The required CI flow is:

1. Check out the exact commit.
2. Install the supported SDK.
3. Restore dependencies.
4. Build the complete solution in Release configuration.
5. Run the full automated suite without rebuilding.
6. Publish readable test results even when a test fails.
7. Run repository spelling and documentation checks.

Local and CI commands should be equivalent so failures can be reproduced
without GitHub-specific behavior.

A pull request must not merge until required build, test and documentation
checks are green. A flaky retry is not evidence of correctness; the cause must
be fixed or the instability explicitly tracked and isolated.

## Coverage policy

Code coverage is a diagnostic tool, not the definition of correctness.

Coverage reports are useful for finding unexercised branches, especially in:

- Core commands and rule validators.
- Error and bankruptcy paths.
- Save/load validation.
- Console navigation and cleanup.
- Infrastructure failure handling.

The repository does not need an arbitrary percentage threshold merely to
increase line execution. New behavior must instead have meaningful assertions
for its success, rejection and state-transition paths.

If a coverage threshold is introduced later, it must be based on an observed
baseline, exclude generated code and must not encourage low-value tests that
only execute lines.

## Manual smoke testing

Manual testing supplements automation for terminal behavior.

For Console changes, verify:

1. The application starts through the documented command.
2. Menus respond to arrows, Enter and permitted Escape.
3. A new game can be configured and rendered.
4. At least one complete turn can be played.
5. A Core decision can be answered.
6. Save and load can be selected and the match can continue.
7. Winner and errors are readable.
8. Exit or match replacement cleans up terminal and subscriptions.

Rule correctness should not rely only on a manual playthrough. Any discovered
defect must receive an automated regression test.

## Current implementation

The current solution has one `Monopoly.Tests` project targeting .NET 10. It
references `Monopoly.Core`, `Monopoly.Console` and `Infrastructure`.

The project currently uses:

- xUnit.
- Moq.
- Microsoft.NET.Test.Sdk.
- xUnit Visual Studio runner.
- Coverlet collector.

The suite currently contains 290 passing tests split between `CoreTests`,
`InfrastructureTests` and `ConsoleTests`.

Current Core coverage includes:

- Data and board construction.
- Dice.
- Chance and Community Chest cards.
- Jail operations.
- Game-handler calculations.
- Property and board eligibility queries.
- Exact payments and actual debt amounts.
- GO wrapping.
- Doubles and Jail release.
- Bankruptcy and winner state.
- Version 1 state mapping without physical storage.
- Public aggregate encapsulation and non-mutable collection contracts.
- Successful and rejected building and mortgage commands, including atomic
  rejection and foreign-object validation.
- Complete Version 1 candidate validation and failure atomicity.
- Static event-subscription replacement.
- Resumable purchase and Jail decisions, immutable snapshots, stable IDs and
  exactly-once continuation through chained decisions.
- Atomic typed rejection of malformed, stale, duplicate and disallowed
  decision responses.
- Detached primitive-only phase, decision and continuation projections.
- Match-scoped random request validation, typed source failures, atomic
  multi-die preparation and purpose separation.
- Deterministic Fisher–Yates deck ordering, shuffle-free Version 1
  reconstruction and isolation between simultaneous matches.

State-heavy Core tests use the internal `GameTestBuilder`. It starts from a
detached Version 1 DTO, applies explicit test arrangements, injects decisions or
a scripted match random source when needed, and always constructs the live match through
`GameStateV1Mapper`'s validated reconstruction path. Tests do not arrange live
state through public setters or mutable aggregate collections.

Current Console coverage includes:

- Arrow-key index movement.
- Menu selection.
- Confirmation and player-count input.
- Board, card, player and log printing.
- String formatting.
- Calls recorded through mocked Console abstractions.
- Injected save-store use and typed save/load error presentation.
- Purchase/Jail prompt mapping, configured Jail values, typed rejection display
  and synchronous driving through multiple Core results.

Current Infrastructure coverage includes:

- Existing Version 1 JSON compatibility and stable wire representation.
- Missing, invalid, incompatible and inaccessible storage classification.
- Atomic file creation and replacement.
- Preservation of an existing save after write, flush or promotion failures.
- Rejection of awaiting-decision Version 1 saves before an existing file is
  touched.

Presentation-contract coverage verifies token grammar, deterministic immutable
catalogs, duplicate or conflicting entries, missing references and
frontend-neutral public Core signatures. Behavioral comparisons run the same
authoritative match with different synthetic text, symbol, color and layout
metadata and assert identical movement, purchases, fees, decisions and Version
1 state. Version 1 regression tests also assert that no presentation catalog
fields enter the established wire representation.

Issue #74 adds fingerprint-specific tests when JSON profiles become
authoritative. Issues #72–#73 own generic board, deck and domain-ID structure
tests, while #77 owns full generic Console projection coverage.

The current GitHub build workflow:

- Runs on pushes to all branches and on pull requests.
- Installs the stable .NET SDK selected by `global.json`.
- Restores and builds the complete solution in Release configuration.
- Runs the complete test suite in Release configuration without rebuilding.
- Uploads readable HTML test results after successful and failed test runs.

A separate workflow checks spelling. Coverlet is referenced but coverage is not
currently collected or published by CI.

## Current limitations

The current suite does not yet fully provide:

- Systematic UK Classic, US Classic and Custom profile contracts.
- Stable card-identity and held-card round trips.
- Complete auction, rent-claim, trade, building and mortgage flow coverage.
- Complete multi-player debt and bankruptcy settlement scenarios.
- Physical pending-decision round trips; Version 1 rejects pending state and
  the primitive Version 2 projections are tested without storage.
- A deterministic automated match from setup to winner.
- Published code-coverage diagnostics.

Some lower-level rule tests still exercise internal Core primitives through the
test assembly's friend boundary; externally observable integration behavior is
also covered through `Game.PlayTurn()` and the public asset commands. Several
`ConsoleWrapperTests` exercise a mock of `IConsoleWrapper` rather than the
concrete wrapper or a consuming component. `TablePieceInputTests`,
`PropertySquareTests` and `TaxSquareTests` are currently empty.

These limitations describe current test maintenance needs; they do not weaken
the normative requirements for new or changed behavior.

## Completion checklist

Before reporting a change complete:

1. Add or update tests for every changed behavior.
2. Add a regression test for every defect fixed.
3. Run focused tests while developing.
4. Run the full Release build.
5. Run the full automated suite.
6. Review failures, skipped tests and warnings.
7. Perform relevant manual Console checks.
8. Review the final diff for accidental test weakening or unrelated changes.
9. Confirm CI uses and passes the same required boundaries.
10. Report tests, warnings and checks that could not be performed.

## Related documentation

- [architecture.md](architecture.md) defines production boundaries.
- [game-flow.md](game-flow.md) defines match and turn transitions.
- [game-rules.md](game-rules.md) defines shared rule behavior.
- [save-format.md](save-format.md) defines persistence contracts.
- [console-frontend.md](console-frontend.md) defines frontend responsibilities.
- [development-workflow.md](development-workflow.md) defines pull-request and
  merge requirements.
