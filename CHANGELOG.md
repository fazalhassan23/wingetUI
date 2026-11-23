# Changelog

All notable user-facing changes are recorded here during release preparation. The project follows [Semantic Versioning](docs/versioning-and-releases.md) once releases begin.

## [Unreleased]

## [0.1.0] - 2026-11-23

### Added

- Windows desktop interface for Updates, Search, Installed, and Activity workflows.
- Multi-selection, select-all support, confirmation dialogs, serial package operations, and row-level progress.
- Administrator restart option for requesting silent installer mode where supported.
- Dark default theme with a system-theme toggle and high-contrast fallback.
- Embedded WingetUI application icon and a self-contained single-file Windows executable.
- Complete user, contributor, architecture, testing, troubleshooting, security, privacy, local-data, workflow, and release documentation.

### Changed

- Refactored the application into core policy, application services, presentation workflows, and WinGet adapter components without changing user-facing behavior.
- Revalidates exact package identity, source, installed version, and target version immediately before execution.

### Fixed

- Preserved unfinished activity entries beyond the terminal-history limit.
- Recovered interrupted work as unknown instead of retrying automatically.
- Continued batch processing after package-local failures while stopping after backend availability failures.
- Preserved operation outcomes on update rows after inventory refresh.
- Improved dark-theme readability and added download size, speed, ETA, and installation percentage where WinGet supplies progress data.

Release preparation assembles detailed fragments from `.changes/` into this file. Published history must not be rewritten.
