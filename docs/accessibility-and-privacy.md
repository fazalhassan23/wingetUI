# Accessibility and privacy

## Accessibility

WingetUI uses Windows system fonts, system color resources, keyboard access keys, named grids and controls, and polite live status announcements. Windows high contrast is detected at startup and replaces the custom dark resources with system colors.

The minimum application size is 920 by 540 pixels. Package tables support keyboard row navigation, and selection checkboxes have accessible names tied to package names.

Current limitations:

- Formal screen-reader testing is still required.
- Focus order, scaling above 200%, localization expansion, and keyboard-only installer workflows need controlled verification.
- Per-package progress depends on information supplied by WinGet and may be indeterminate.

Accessibility regressions should include the Windows version, display scale, input method, assistive technology and version, control name, expected announcement or focus behavior, and observed result.

## Privacy

WingetUI has no account system, analytics, advertising, or application telemetry. It communicates with App Installer and the package sources and download locations selected by WinGet.

Local activity history can contain package names, identifiers, sources, versions, timestamps, actions, and result codes. This may reveal installed software and should be treated as private user data. It remains under `%LOCALAPPDATA%\WingetUI` for the active Windows account and is not transmitted by WingetUI.

Copied activity details enter the Windows clipboard and may be visible to other software with clipboard access. Review copied text before sharing it in a public issue.

Deleting `%LOCALAPPDATA%\WingetUI` while WingetUI is closed removes its retained activity history. This does not remove WinGet, App Installer, installed packages, or logs owned by Windows and individual installers.
