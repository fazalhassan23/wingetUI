# Configuration and local data

WingetUI currently has no command-line configuration file, registry settings, environment variables, telemetry, or remote user account.

## Runtime defaults

| Setting | Current behavior |
| --- | --- |
| Source | WinGet community source (`winget`) |
| Theme | Dark at startup; system colors on user toggle |
| Package execution | Serial, one operation at a time |
| Normal install mode | Interactive |
| Elevated install mode | Silent request where supported |
| Search result limit | 100 |
| Completed activity retention | Latest 100 terminal entries |
| Unfinished activity retention | All entries |
| Automatic retry | Disabled |

## Activity data

Activity is written to `%LOCALAPPDATA%\WingetUI\activity.json` for the Windows account running the application. Writes use a temporary file followed by replacement to reduce the chance of partial JSON.

The file can contain package names, package identifiers, sources, installed and target versions, timestamps, operation type, status, and WinGet result detail. It does not contain credentials.

Delete the `WingetUI` directory under LocalAppData to clear history. Do this only while the application is closed. The file is not synchronized by WingetUI and has no migration or recovery tool beyond tolerant loading of valid records.

## Process arguments

`--elevated-restart` is an internal handoff argument used after **Restart as administrator**. It allows the new process to wait briefly for the previous per-user instance to exit. It is not a general user configuration option.

## Build configuration

The application targets `net10.0-windows10.0.26100.0`, `win-x64`, and self-contained publishing. The App Installer integration package version is declared in `src/WingetUI.App/WingetUI.App.csproj`.
