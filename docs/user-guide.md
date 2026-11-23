# User guide

WingetUI manages packages from the WinGet community source through a Windows desktop interface. It provides four areas: Updates, Search, Installed, and Activity.

## Requirements

- Windows 11 x64, build 26100 or later.
- Microsoft App Installer registered for the Windows account running WingetUI.
- Access to the WinGet community source.
- An internet connection for catalog search and package downloads.

The distributed self-contained build includes the .NET runtime. Building from source requires the .NET 10 SDK.

## Start and remove the application

Extract the complete distribution ZIP, keep its files together, and run `WingetUI.exe`. WingetUI allows one instance per Windows account.

The application is portable. Remove it by deleting the extracted directory. To also remove activity history, delete `%LOCALAPPDATA%\WingetUI`.

## Updates

WingetUI opens on Updates and refreshes installed-package inventory. Only packages correlated with the `winget` source, with a known target version and an available update, are eligible.

1. Select packages using the checkbox at the left of each row.
2. Use the header checkbox or **Select all** for the entire list. A mixed checkbox means only some rows are selected.
3. Select **Upgrade selected**.
4. Review package identifiers, versions, and sources in the confirmation dialog.
5. Confirm to place the packages into the serial queue.

Packages run one at a time. Each update row shows its current phase and progress when WinGet supplies a percentage. During downloads, WingetUI also shows transferred size, a smoothed transfer rate, and estimated time remaining when WinGet supplies byte counts. Speed and time remaining are estimates and can change as the connection or package server changes.

## Search and install

Enter a package name or identifier on Search. Search begins after a short pause and newer queries replace older pending searches.

Select one result and choose **Review selected package**. WingetUI will reject unresolved, already installed, stale, or otherwise ineligible targets. Review and confirm the exact package before it enters the queue.

## Installed packages

Installed shows the inventory visible to WinGet for the current Windows account. It is informational; uninstall is not currently implemented.

## Activity

Activity records queued, resolving, running, succeeded, reboot-required, failed, cancelled, unknown, and skipped operations.

- **Remove queued item** cancels an item only before execution begins.
- **Copy details** copies package identity, versions, timestamps, status, and result detail.
- Closing during an active operation warns that its final outcome may be unknown.
- After an interrupted session, unfinished entries become `Unknown`; WingetUI never retries them automatically.

All unfinished entries and the newest 100 completed entries are retained locally.

## Administrator mode and prompts

WingetUI normally runs with the current user's privileges and requests interactive installer mode. **Restart as administrator** closes and reopens the app after Windows displays a single elevation confirmation. You must refresh, select, and confirm packages again.

An elevated session requests silent installer mode. This reduces prompts for installers that support it, but WingetUI cannot suppress prompts required by an installer, Windows policy, license agreement, or reboot workflow.

Running under a different administrator account can change visible packages, sources, and history location.

## Theme and accessibility

Dark mode is the default. **Toggle theme** switches to Windows system colors for the current session. The selection is not persisted. When Windows high contrast is active at startup, WingetUI uses system colors.

Navigation labels and common actions expose keyboard access keys. Status and operation messages use polite accessibility announcements.

## Current limitations

- Only the WinGet community source is supported.
- Uninstall, source management, automatic retry, scheduled upgrades, and background updating are not implemented.
- Active WinGet operations cannot currently be cancelled from the interface.
- Some source agreements must be accepted with official WinGet tools.
- Installer prompt behavior is controlled by the package installer and Windows, not WingetUI alone.
