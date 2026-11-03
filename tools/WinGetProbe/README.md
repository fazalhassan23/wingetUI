# WinGet COM probe

This self-contained .NET 10 x64 console probe activates the registered WinGet COM server and connects to the community catalog. It does not invoke package mutations.

From the repository root with the .NET 10 SDK:

```powershell
dotnet publish tools/WinGetProbe/WinGetProbe.csproj -c Release -r win-x64 --self-contained true
& ./tools/WinGetProbe/bin/Release/net10.0-windows10.0.26100.0/win-x64/publish/WinGetProbe.exe
```

The signed-in host reported Windows build 26200 and App Installer 1.29.380.0 x64. A PowerShell check activated the production COM class successfully. The probe pins interop 1.29.380 as the candidate for this baseline. The PowerShell result alone does not validate the .NET projection, catalog connection, or package operations.

Record the App Installer version, Windows build, locale, process elevation, and probe output for each run. Catalog search, inventory and CLI parity, controlled install and upgrade, cancellation, UAC denial, alternate account and locale, and SDK-free launch still need verification before documenting a supported baseline.

The self-contained probe published with SDK 10.0.403 and returned `Community catalog connection: Ok` on that host in an unelevated x64 process.

The expanded probe found one exact Microsoft.PowerToys match, matching the CLI package ID. The correlated installed catalog returned 150 entries, and WinGet flagged 27 as having updates. These counts describe this host at the time of the test. Version parity and operation eligibility still need verification. Indexed access to the projected result collection worked. The initial enumeration path returned 0x80004002 before the package details were printed.
