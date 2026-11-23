# WingetUI

WingetUI is a Windows 11 x64 desktop client for the WinGet community source. It opens on Updates and includes Search, Installed, and Activity. Install and upgrade requests show the exact target and version before they enter a serial queue.

## Run a local build

Use Windows 11 x64 with current App Installer registered for your account. Extract the entire ZIP and double-click **WingetUI.exe**. The ZIP bundles .NET. Keep the supporting files next to the executable. Delete the extracted folder to remove the application. Delete the WingetUI folder under LocalAppData to remove its local activity history.

## Build and validate

Install the .NET 10 SDK. From the repository root:

```powershell
dotnet publish src/WingetUI.App/WingetUI.App.csproj -c Release -r win-x64 --self-contained true -o .local/publish
Compress-Archive -Path .local/publish/* -DestinationPath .local/WingetUI-win-x64.zip -Force
dotnet publish tests/WingetUI.Smoke -c Release -r win-x64 --self-contained true -o .local/smoke
& ./.local/smoke/WingetUI.Smoke.exe
```

The smoke test queries the live community catalog and installed inventory. It makes no package changes. CI compiles the test and publishes a ZIP. Run live checks under the Windows account whose App Installer registration the app should use.

## Architecture

```mermaid
flowchart LR
    WPF[WPF views and view model] --> Core[Core contracts]
    WPF --> History[Local activity history]
    Adapter[WinGet adapter] --> Core
    Adapter --> COM[App Installer COM server]
    COM --> Catalog[WinGet community catalog]
    COM --> Installer[Package installer]
```

WPF owns confirmation dialogs and presentation state. Application services own the serial queue and local history, while the adapter keeps COM types on its side of the contract and rechecks package identity and version before calling WinGet. On restart, unfinished operations appear as unknown and inventory refreshes. The application never retries them automatically. See [the architecture guide](docs/architecture.md) and [refactoring map](docs/refactoring.md).

## Troubleshooting and verification status

If inventory is unavailable, run `winget --version` in the same Windows account and check that App Installer is installed and current. If search works but inventory is unavailable, refresh before submitting any package request. Use official WinGet commands for agreements that cannot be completed in the app.

The application starts unelevated and asks WinGet for interactive installer mode. Host validation covers Windows build 26200 and App Installer 1.29.380.0. The deployment targets Windows build 26100 and uses WinGet ComInterop 1.29.380. Controlled installs, upgrades, UAC outcomes, active cancellation, localization, and SDK-free clean-machine verification are still pending.

Use **Restart as administrator** before selecting a batch to approve elevation for the app session. The app refreshes in the new process and requires a fresh confirmation. In an elevated session, WingetUI requests silent installer mode. Actual prompt behavior depends on WinGet and the installer.

Administrator sessions use WinGet's in-process COM engine. To check catalog access without installing anything, run the published smoke test from an elevated PowerShell with --check-in-process. Per-installer UAC behavior still requires an elevated batch test.
During a batch, Updates keeps its rows visible and shows per-package progress. Select an Activity entry and use **Copy details** for timestamps, versions, and the result.
The desktop layout uses a navigation sidebar, larger package rows, and a highlighted primary upgrade action. Windows system colors supply the interface palette.
Use the checkbox above the update rows to select or clear all updates. A filled intermediate state indicates that only some updates are selected.
## Audit corrections and compact theme

Dark mode is enabled at launch. **Toggle theme** switches to the Windows system palette for the current session. When Windows high contrast is enabled at startup, the app uses system colors.

Package rows are 36 pixels high. Update outcomes remain associated with packages that remain in the refreshed list. Activity retains the full result for packages removed from Updates after a successful upgrade.

History retains every unfinished operation and the latest 100 terminal records. Invalid null records are ignored during recovery. Interrupted operations remain unknown and require inventory review. Package-local exceptions continue the batch. Catalog availability failures stop remaining work.

Run deterministic checks without changing installed packages:

```powershell
dotnet run --project tests/WingetUI.Tests -c Release
```

These cover unknown-version matching, large-batch persistence, null history, interrupted operations, continuation after a package exception, and row outcomes after refresh.
