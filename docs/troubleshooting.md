# Troubleshooting

## Inventory is unavailable

1. Run `winget --version` in PowerShell under the same Windows account.
2. Confirm Microsoft App Installer is installed and current.
3. Confirm the WinGet community source is available with `winget source list`.
4. Restart WingetUI and select **Refresh**.

Displayed results may be stale after a refresh failure. WingetUI prevents install or upgrade submission until a current inventory has loaded.

## Search works but installed packages or updates do not

Search uses the community catalog, while inventory correlation depends on App Installer's local catalog for the current account. Verify account context, App Installer registration, and source health. An elevated process running under another account may see different packages.

## Error 0x80040154 or COM activation failure

App Installer may not be registered for the current account or its COM server may be unavailable. Update or reinstall App Installer, sign out and back in if registration has changed, and rerun the smoke test.

## Administrator mode still shows prompts

Administrator mode requests WinGet's silent installer mode; it does not bypass Windows security or installer behavior. A package may still require UI for agreements, configuration, policy, child processes, or reboot handling. Verify the same package with official WinGet commands from an elevated PowerShell before treating the prompt as a WingetUI defect.

## An operation shows Unknown after restart

WingetUI cannot prove the final state of work interrupted by process exit. Refresh inventory and verify the installed version before retrying. Unknown operations are never retried automatically.

## A package was skipped

WingetUI re-resolves the exact identifier and source immediately before execution. It skips work when the installed version, available version, eligibility, or catalog match differs from the confirmed state. Refresh and review the package again.

## Remaining packages were skipped after an error

A catalog or backend availability failure stops remaining queue work because further operations are unlikely to succeed. Package-local installer failures normally allow the queue to continue.

## Activity history cannot be loaded or saved

Check access to `%LOCALAPPDATA%\WingetUI` and available disk space. Close WingetUI before moving or deleting `activity.json`. If the JSON is damaged, preserve a copy for diagnosis and remove the original to start with empty history.

## A second instance will not open

Only one instance is allowed per Windows account. Bring the existing window forward or close it. After an administrator restart, the new process waits up to ten seconds for the original instance to release ownership.

## Information to include in a bug report

- WingetUI commit or release version.
- Windows build and architecture.
- Output of `winget --version`.
- Normal or administrator session.
- Sanitized package identifier, source, versions, status, and error code.
- Whether the deterministic and smoke tests pass.

Do not publish private package metadata, account identifiers, credentials, or raw activity history containing sensitive information.
