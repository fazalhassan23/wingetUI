# Development guide

## Prerequisites

- Windows 11 x64.
- .NET 10 SDK.
- Current Microsoft App Installer registered for the development account.
- Git and, for repository integration, GitHub CLI authentication.

## Repository layout

```text
src/WingetUI.Core/      Framework-independent package and operation rules
src/WingetUI.App/       WPF presentation, state, queue, and persistence
src/WingetUI.WinGet/    App Installer COM adapter
tests/WingetUI.Tests/   Deterministic regression executable
tests/WingetUI.Smoke/   Read-only live WinGet verification
tools/WinGetProbe/      Diagnostic catalog probe
docs/                   User, contributor, architecture, and operations guides
.changes/               Per-change changelog fragments
```

See [architecture](architecture.md) for boundaries and dependency direction.

## Restore and build

From the repository root in PowerShell:

```powershell
dotnet restore src/WingetUI.App/WingetUI.App.csproj
dotnet build src/WingetUI.App/WingetUI.App.csproj -c Release
```

Run the application from source:

```powershell
dotnet run --project src/WingetUI.App/WingetUI.App.csproj -c Release
```

Run under the same Windows account whose App Installer registration and package inventory you intend to test. Starting from an elevated terminal changes execution mode and can change the account context.

## Publish a local package

```powershell
dotnet publish src/WingetUI.App/WingetUI.App.csproj -c Release -r win-x64 --self-contained true -o .local/publish
Compress-Archive -Path .local/publish/* -DestinationPath .local/WingetUI-win-x64.zip -Force
```

`.local`, `bin`, and `obj` are ignored build output and must not be committed.

Regenerate the multi-size Windows icon after changing its source PNG:

```powershell
./tools/Convert-PngToIco.ps1 -InputPath src/WingetUI.App/Assets/WingetUI.png -OutputPath src/WingetUI.App/Assets/WingetUI.ico
```

## Debugging boundaries

- Start with `MainViewModel.Inventory`, `Search`, or `Operations` for workflow state.
- Inspect `PackageOperationQueue` for ordering, persistence timing, and failure isolation.
- Inspect `WinGetCatalogClient` for catalog mapping and `WinGetOperationExecutor` for final target validation.
- COM calls must run on the MTA helper and COM objects must remain inside the adapter.
- Reproduce catalog problems with the read-only smoke test before changing installer behavior.

## Documentation obligations

Behavior changes require updates to the user guide and troubleshooting guidance. Architecture changes require diagrams. Persistence changes require schema and recovery documentation. User-visible changes require a `.changes` fragment.
