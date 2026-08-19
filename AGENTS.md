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
