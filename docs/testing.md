# Testing and verification

WingetUI uses deterministic regression checks, a read-only live WinGet smoke test, build validation, and manual installer verification.

## Deterministic regression checks

```powershell
dotnet run --project tests/WingetUI.Tests -c Release
```

These checks do not change installed packages. They cover:

- Matching unknown and changed installed versions.
- Persistence of more than 100 unfinished operations.
- Continuation after a package-local exception.
- Preservation of row outcomes after refresh.
- Ignoring invalid null history records.
- Recovery of interrupted operations as unknown.
- Download speed, estimated-time calculation, and readable progress formatting.

## Build checks

```powershell
dotnet build src/WingetUI.App -c Release
dotnet build tests/WingetUI.Smoke -c Release
dotnet format src/WingetUI.App/WingetUI.App.csproj --verify-no-changes --no-restore
./tools/Validate-Documentation.ps1
git diff --check
```

## Live smoke test

```powershell
dotnet run --project tests/WingetUI.Smoke -c Release
```

The smoke test queries the community catalog and installed inventory, then submits a deliberately stale target that must be rejected before installer execution. It must not install, upgrade, or uninstall anything.

To verify elevated in-process activation, run the published smoke executable from an elevated PowerShell with `--check-in-process`.

## Manual verification checklist

Test normal and administrator sessions separately:

1. Refresh Updates and Installed.
2. Search rapidly and confirm an older query cannot replace newer results.
3. Select one, several, and all updates; verify the mixed select-all state.
4. Review confirmation text before enqueueing.
5. Verify serial execution and row-level progress.
6. Remove an item that is still queued.
7. Close during active work and verify the warning.
8. Restart after interrupted work and verify unknown recovery without retry.
9. Copy activity details.
10. Toggle themes and start in Windows high contrast.
11. Cancel and accept the administrator restart prompt.
12. Verify a second instance is rejected for the same Windows account.

Actual install and upgrade verification changes the machine. Record the exact packages, expected versions, Windows/App Installer versions, elevation state, and observed UAC or installer prompts. Do not automate destructive package testing in CI.

## Continuous integration

The Windows workflow runs deterministic checks, builds the smoke test, validates required documents and internal links, publishes a self-contained application, validates required artifacts, creates a ZIP, and uploads it as a workflow artifact. A CI artifact is not a GitHub release or production deployment.
