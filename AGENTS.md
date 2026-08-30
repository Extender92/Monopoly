# Agent instructions

These instructions apply to every agent working in this repository.

## Before starting work

- Read the root `README.md` and the relevant documentation linked from it before
  changing code, tests, workflows or documentation.
- Inspect the relevant implementation before proposing or making changes.
- Check the current branch, Git status, staged changes and unstaged changes.
- Preserve existing user changes. Do not reset, discard, overwrite or reformat
  unrelated work.
- Check relevant open GitHub Issues before changing an area. Issues are the
  source of truth for current defects and planned work.
- Perform implementation work on an existing matching focused branch or create
  one according to the documented workflow. Do not implement changes directly
  on `main`.

## Work plan and issue selection

- Before selecting implementation work, read GitHub issue #59 and its current
  execution-order section.
- Follow milestones in order from M1 through M6.
- Within #59's execution order, select the first open issue whose declared
  dependencies are complete; issue numbers alone do not define execution
  order.
- Read the selected issue and its direct dependencies and related issues before
  changing the repository.
- Verify the current GitHub state because completed work may have changed the
  next eligible issue.
- Do not create or rewrite issues, or change their priority or milestone,
  merely to resolve a planning ambiguity without explicit owner approval.
- If #59, milestone metadata and an issue dependency conflict, report the
  conflict before implementation.
- These planning instructions apply only to this legacy repository. During #58,
  replace them with the new repository's planning entry point before creating
  the clean root commit.

## While working

- Follow the contracts and boundaries defined by the relevant documentation.
- Keep the work focused on the requested objective and its acceptance criteria.
- Do not add unrelated cleanup merely because nearby files are open.
- Distinguish intended target behavior from the current implementation.
- Do not claim that documented target behavior is implemented without verifying
  the code and tests.
- Update tests and documentation when a behavior or contract changes.
- Add a regression test for every corrected defect.
- Do not remove, skip or weaken tests merely to make verification pass.
- Do not use destructive Git or filesystem operations that could remove user
  work.

## Temporary verification cleanup

- Temporary files and directories created by the agent solely for verification
  may be removed when they are no longer needed. This is standing owner
  approval only for those exact agent-created targets, not for general cleanup.
- Before removal, resolve and inspect the absolute target. For a system-temp
  target, verify that it is inside the system temporary directory and has the
  task-specific name created during the current work. For a workspace target,
  verify that it is untracked, agent-created and unrelated to user changes.
- Never use a workspace root, system-temp root, home directory, unresolved
  variable, wildcard or broad recursive target for this cleanup. Perform path
  verification and removal in the same shell with literal paths.
- Report what was removed. If a higher-level tool or safety policy blocks the
  operation, leave the target untouched and report its exact path to the owner.

## Git and GitHub actions

- Follow `docs/development-workflow.md` for branches, commits, pull requests,
  issue references, squash merges and releases.
- Read-only inspection of repository history, pull requests and issues is
  allowed when relevant to the task.
- Do not create commits, push branches, open pull requests, merge, delete
  branches, create tags or publish releases unless the user explicitly requests
  that action.
- Do not create, edit, comment on or close GitHub Issues unless the user
  explicitly requests that action.
- Use `Closes #N` only when all acceptance criteria for issue `#N` are satisfied.
- Do not reuse a closed issue as the completion reference for later follow-up
  work.

## Verification

Before reporting work as complete:

1. Review the final Git status and complete diff.
2. Check for accidental, unrelated, generated or temporary files.
3. Validate affected documentation links and formatting.
4. Run the repository's required restore, Release build and Release test
   commands from the root unless the task cannot affect the repository output.
5. Perform relevant manual or interactive checks when automated tests do not
   cover the changed behavior.
6. Report warnings, failures, skipped tests and checks that could not be run.

Do not hide or dismiss verification failures. Investigate them and clearly
separate failures that existed before the current work from failures introduced
by it.

## Completion report

State:

- What changed.
- The current branch.
- Whether a commit was created.
- Build warnings and errors.
- Passed, failed and skipped test counts.
- Manual or external checks performed.
- Checks that could not be performed.
- Remaining limitations or follow-up work.
