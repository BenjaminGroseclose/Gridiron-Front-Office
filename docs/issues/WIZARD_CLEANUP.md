# Issue Template

Use this template to track bugs, improvements, and feature work across GridironFrontOffice.

## Issue Metadata

- **Issue ID**: `ISSUE-WIZARD-DB-CLEANUP`
- **Title**: Clean up temporary SQLite DB when league wizard exits early
- **Type**: `Bug`
- **Priority**: `P1`
- **Severity** (if bug): `High`
- **Status**: `Backlog`
- **Area**: `Persistence | Application | UI`
- **Created**: 2026-03-06
- **Related Docs/Issues/PRs**: `docs/features/LEAGUE_SETUP.md`

## Summary

If a user exits the league creation wizard before completion, the partially created SQLite database file is not removed. This leaves orphaned DB files on disk and can cause confusion or stale state on the next setup attempt.

## Background / Context

The league setup flow appears to create backing persistence artifacts before the wizard is fully committed. Users can close, cancel, or navigate away mid-process, but cleanup logic is currently missing or incomplete for those early-exit paths.

## Current Behavior

When the wizard is abandoned part-way through league creation, a SQLite DB file remains on disk even though league setup was never completed.

## Expected Behavior

If league creation does not complete successfully, any temporary or partially initialized SQLite DB file should be deleted automatically.

## Steps To Reproduce (for bugs)

1. Start the app and begin the league creation wizard.
2. Proceed far enough that the SQLite DB file is created.
3. Exit/cancel/close the wizard before final confirmation.
4. Inspect the data folder and observe that the DB file still exists.

## Scope

- **In Scope**: Add cleanup for all wizard-abort paths (cancel button, navigation away, window close, unexpected wizard termination before commit).
- **Out of Scope**: Changing final league creation behavior after successful completion.

## Proposed Approach

Introduce explicit lifecycle handling for the wizard session with a "creation committed" state. If the wizard exits without commit, invoke persistence cleanup to remove the newly created SQLite file and any related temporary artifacts. Ensure cleanup runs from all known exit points and is safe if files are already missing.

## Acceptance Criteria

1. Abandoning the wizard at any step after DB creation removes the SQLite DB file.
2. Completing the wizard successfully keeps the DB file and league data intact.
3. Cleanup logic is idempotent and does not throw if the file is already deleted.
4. No regressions in league setup flow, startup, or subsequent wizard runs.

## Technical Notes

- **Potential Files/Modules**: `src/Application/LeagueWizardService.cs`, `src/Persistence/DatabaseConnectionFactory.cs`, wizard UI flow files under `src/Desktop/` or `src/UI/`.
- **Data/Schema Changes**: None expected.
- **Dependencies**: Existing wizard navigation/state services and DB file creation path logic.
- **Risks**: Accidental deletion of a valid DB if commit state is tracked incorrectly; race conditions on app close.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] No new build warnings/errors
- [ ] Documentation updated (if applicable)
- [ ] PR linked and reviewed

## Notes / Open Questions

- Should cleanup run synchronously on cancel, or be queued as best-effort background cleanup on app shutdown?
- Do we need a startup sweep for stale temporary DB files from previous crashes?
