# Development workflow

## Purpose

This document defines how changes move from an issue to a branch, pull request,
squash merge and optional release in the Monopoly repository.

The workflow keeps `main` stable, makes each change reviewable and preserves one
clean logical commit per merged pull request.

This is a repository workflow for contributors. Architectural, game-rule,
testing and persistence requirements are defined in their focused documents.

## Workflow principles

- Do not implement changes directly on `main`.
- Start from the latest clean `origin/main`.
- Keep one branch and pull request focused on one coherent objective.
- Connect work to the issue it actually resolves or references.
- Preserve unrelated local changes.
- Add tests and documentation with the behavior they protect.
- Verify the complete Release build and test suite before review and merge.
- Require green repository checks.
- Squash merge so `main` receives one logical commit per pull request.
- Create release tags only for intentional stable milestones.

A small pull request is not defined by line count. It is small when reviewers
can understand one objective, its implementation, tests and consequences
without also evaluating unrelated work.

## Issues

A significant feature, defect, refactor or architectural change should begin
with a GitHub issue.

An actionable issue should describe:

- The problem or goal.
- Why it belongs in the project.
- Required behavior.
- Scope and explicit exclusions where useful.
- Acceptance criteria.
- Relevant architecture or rule decisions.
- Related or blocking issues.
- Required tests and documentation.

Defects should include observed and expected behavior and enough reproduction
information to create a regression test.

An issue may be split when its acceptance criteria contain independent changes
that can be implemented and reviewed separately. Use an umbrella issue to track
the shared objective and focused child issues for delivery.

Small obvious maintenance changes do not always require a new issue, but their
pull request must still explain the problem and verification.

GitHub Issues are the source of truth for current defects and planned work.
Permanent documentation should describe the intended system rather than become
a duplicate list of open issues.

## Preparing the worktree

Before creating a branch:

```text
git status --short
git switch main
git pull --ff-only origin main
```

The worktree should be understood and clean before switching or creating a
branch. Existing uncommitted changes belong to their author and must not be
discarded, overwritten or mixed into an unrelated objective.

If unrelated changes are present, finish, commit or safely separate that work
before starting another branch.

`--ff-only` prevents an accidental merge commit while updating local `main`.

## Branch creation and naming

Create a focused branch from the updated `main`:

```text
git switch -c <type>/<short-description>
```

Supported branch prefixes are:

- `feature/` for new behavior.
- `fix/` for defect corrections.
- `refactor/` for structural changes without intentional behavior changes.
- `docs/` for documentation.
- `test/` for focused test maintenance.
- `chore/` for dependencies, tooling and repository maintenance.
- `upgrade/` for significant framework, SDK or platform upgrades.

Examples:

```text
feature/classic-property-auctions
fix/jail-release-state
refactor/rule-profile-model
docs/project-documentation-and-agent-guidelines
test/core-game-flow-coverage
chore/update-github-actions
upgrade/dotnet-10-csharp-14
```

Use lowercase words separated by hyphens. Keep the name short but specific.

The issue number is optional in the branch name. The authoritative issue link is
recorded in the pull request.

## Scope while working

One branch may include production code, tests and documentation when they all
serve the same objective.

Do not add opportunistic unrelated cleanup merely because a file is already
open. Record separately discovered work in an issue and handle it in another
branch unless it is required to complete the current acceptance criteria.

Before editing:

- Read the relevant implementation and documentation.
- Inspect `git status` and the existing diff.
- Identify the correct project boundary.
- Review related open issues.
- Confirm the intended acceptance criteria.

During implementation:

- Keep Core, frontend and Infrastructure responsibilities separate.
- Preserve backward compatibility unless the issue explicitly changes it.
- Add regression tests for defects.
- Keep the branch buildable at useful checkpoints.
- Review generated changes and avoid committing build outputs, local saves,
  credentials or editor-specific files.

## Working commits

Multiple local commits are acceptable while developing a branch. They can
represent useful checkpoints, review fixes or intermediate refactoring.

Each working commit should:

- Describe one understandable change.
- Avoid unrelated files.
- Leave the repository in a reasonable state where practical.
- Use a concise imperative or outcome-focused title.

Examples:

```text
Fix jail release state handling
Add Classic property auction flow
Test bankruptcy turn rotation
Document persistence boundaries
```

Avoid vague titles such as:

```text
Updates
Fix stuff
More changes
WIP final
```

The branch may contain several working commits, but squash merge is used so
`main` receives one final logical commit.

## Issue references

Issue references and Git release tags are different concepts.

```text
#27       GitHub issue reference
v0.1.1    Git release tag
```

Use one of the following statements in the pull-request description:

- `Closes #N` when the pull request satisfies the complete issue.
- `Refs #N` when it contributes to an issue without completing it.
- `Related to #N` when the relationship is informative rather than direct.

Use `Closes` only after comparing the final implementation with every
acceptance criterion. A pull request may close multiple issues only when it
fully resolves all of them.

Do not reuse a closed issue number as if a later follow-up closes it again.
Create or reference the issue that describes the follow-up work.

The final squash-commit title may include the issue number:

```text
Document project architecture and workflow (#27)
```

This creates a useful connection in `git log`. It does not replace the closing
statement in the pull-request description.

## Local verification

Run focused tests while working. Before opening or updating a pull request, run
the complete verification from the repository root:

```text
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release --no-build
```

Also:

- Review the complete diff.
- Check for accidental, generated or unrelated files.
- Check warnings, failures and skipped tests.
- Run relevant manual Console smoke tests.
- Validate Markdown links and spelling for documentation changes.
- Verify that any compatibility or migration behavior matches its documented
  contract.

A focused test run does not replace the full suite.

If a required check cannot be run, explain why and what remains unverified in
the pull request. Do not report the change as fully verified.

Detailed expectations are defined in [testing.md](testing.md).

## Reviewing the diff

Before publishing the branch:

```text
git status --short
git diff --check
git diff
git diff --cached
```

When commits already exist, also review the complete branch against `main`:

```text
git diff origin/main...HEAD
```

Confirm:

- Every changed file belongs to the objective.
- Tests assert the intended behavior rather than implementation accidents.
- No existing regression test was weakened without justification.
- Public API changes are intentional and documented.
- Current-state and target-state documentation are clearly distinguished.
- No secret, local save, test result, build artifact or temporary file is
  included.

## Pull requests

A pull request targets `main` from its focused branch.

The title should state the delivered outcome. Avoid prefixes or issue references
that obscure the actual change.

A useful description is:

```markdown
Closes #N

## Summary

- Describe the important result.
- Describe architectural or compatibility decisions.
- Mention relevant documentation.

## Verification

- `dotnet restore`
- `dotnet build --configuration Release`
- `dotnet test --configuration Release --no-build`
- X tests passed
- 0 warnings and 0 errors
- Relevant manual check

## Limitations

- Describe anything intentionally out of scope or not verified.
```

Omit the Limitations section when there are no meaningful limitations.

The summary should explain outcomes rather than provide a file-by-file change
log. Verification must report actual results, not only commands that were
intended to run.

A draft pull request may be used for early feedback, but it is not ready to
merge until the implementation, tests, documentation and checks are complete.

## Updating a branch

If `main` changes before merge, update the branch and rerun verification.

For a private unshared branch, rebasing onto the latest `origin/main` keeps the
branch easy to review:

```text
git fetch origin
git rebase origin/main
```

Do not rewrite a branch other contributors are using without coordination.
Never force-push `main`. If a rewritten personal branch must be pushed, use
`--force-with-lease`, not an unrestricted force push.

Resolve conflicts according to the current architecture and rerun the complete
suite after resolution.

## Review and required checks

Before merge:

- The issue acceptance criteria are satisfied.
- The final pull-request diff has been reviewed.
- Build and test checks are green.
- Spelling and documentation checks are green.
- Required manual checks are complete.
- Review comments are resolved.
- There are no unexplained warnings, failures or skipped tests.
- The branch contains no unrelated work.

A flaky check that passes only after retries is still a defect. Determine and
correct the cause or explicitly isolate and track it before merge.

## Squash merge

Use squash merge for repository pull requests.

This produces one clean commit on `main` even when the working branch contains
several implementation and review commits.

The squash-commit title should:

- Describe the complete outcome.
- Match the repository's concise commit style.
- Reference the issue completed by the change when appropriate.

Example:

```text
Document project architecture and workflow (#27)
```

Review the squash title and body before confirming the merge. Do not leave
temporary commit messages or claim that an issue is closed when the pull request
only references it.

Do not use a merge commit for ordinary repository changes. Do not merge a
feature branch directly from a local checkout as a substitute for the reviewed
pull request.

## Post-merge cleanup

After the pull request is merged:

```text
git switch main
git pull --ff-only origin main
git branch -d <branch-name>
```

Then verify:

- Local `main` matches `origin/main`.
- The worktree is clean.
- The squash commit is present.
- Issues referenced with `Closes` are closed.
- The remote branch has been removed.
- CI for the merged `main` commit is green.

The repository currently deletes merged remote branches automatically. Delete
the local branch only after confirming the merge exists on `main`.

## Releases and tags

A Git tag marks a stable release or meaningful milestone, not an individual
issue.

Tags use the repository's version form:

```text
vMAJOR.MINOR.PATCH
```

Create an annotated tag on the verified merged commit on `main`:

```text
git switch main
git pull --ff-only origin main
git tag -a v0.2.0 -m "Release v0.2.0: description"
git push origin v0.2.0
```

Before tagging:

- Confirm the intended commit is checked out on `main`.
- Confirm required CI is green.
- Confirm the version number has an agreed release meaning.
- Confirm release notes or milestone scope are accurate.

Do not:

- Tag an unmerged feature branch.
- Move an existing published tag to another commit.
- Create a release tag merely because one ordinary issue was closed.
- Use an issue reference such as `#27` as a Git tag.

Documentation-only, test-only and routine maintenance pull requests normally do
not require a release tag unless they are intentionally part of a named
release.

## Documentation-only changes

Documentation follows the same branch, review, verification and squash-merge
workflow as code.

For documentation changes:

- Use a `docs/` branch.
- Keep current implementation and target behavior clearly separated.
- Check every local link.
- Run the repository spelling workflow.
- Run the full build and test suite because documentation can change commands,
  project names and operational expectations.
- Update the root README only when project-level entry information changes.
- Avoid duplicating current GitHub issues in permanent documentation.

## Hotfixes

An urgent defect still uses an issue, focused `fix/` branch, regression test,
pull request and green checks.

Urgency may reduce unrelated cleanup and review latency, but it does not justify
working directly on `main`, skipping regression coverage or bypassing the
squash merge.

If a hotfix becomes a release, tag the verified merged `main` commit after the
pull request is complete.

## Current repository configuration

The repository currently:

- Uses `main` as its default branch.
- Runs build and test checks for pushes and pull requests.
- Runs a separate spelling workflow.
- Allows squash, merge-commit and rebase methods in GitHub settings.
- Automatically deletes merged remote branches.
- Uses annotated release tags such as `v0.1.0` and `v0.1.1`.

The project workflow selects squash merge even though GitHub currently exposes
the other merge methods.

Recent repository work demonstrates the intended focused-branch pattern with
`refactor/`, `chore/` and `upgrade/` branches. This document extends the
same model consistently to features, fixes, tests and documentation.

## Completion checklist

Before merge:

1. The branch has one coherent objective.
2. The correct issue relationship is present.
3. Acceptance criteria are satisfied.
4. Tests and documentation cover the change.
5. The complete diff has been reviewed.
6. Restore and Release build/test commands pass.
7. Warnings, failures and skipped tests are resolved or explained.
8. Relevant manual checks pass.
9. GitHub checks are green.
10. The squash title accurately describes the outcome.

After merge:

1. Update local `main` with `--ff-only`.
2. Confirm the squash commit and issue state.
3. Confirm merged-`main` CI.
4. Remove the local branch.
5. Create a tag only when this merge forms an intentional stable release.

## Related documentation

- [architecture.md](architecture.md) defines project boundaries.
- [game-flow.md](game-flow.md) defines match transitions.
- [game-rules.md](game-rules.md) defines the rule-profile contract.
- [save-format.md](save-format.md) defines persistence compatibility.
- [console-frontend.md](console-frontend.md) defines frontend responsibilities.
- [testing.md](testing.md) defines verification and regression requirements.
