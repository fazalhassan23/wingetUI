# Changelog

All notable user-facing changes are recorded here during release preparation. The project follows [Semantic Versioning](docs/versioning-and-releases.md) once releases begin.

## [Unreleased]

### Added

- Windows desktop interface for Updates, Search, Installed, and Activity workflows.
- Multi-selection, select-all support, confirmation dialogs, serial package operations, and row-level progress.
- Administrator restart option for requesting silent installer mode where supported.
- Dark default theme with a system-theme toggle and high-contrast fallback.

### Changed

- Refactored the application into core policy, application services, presentation workflows, and WinGet adapter components without changing user-facing behavior.

### Fixed

- Preserved unfinished activity entries beyond the terminal-history limit.
- Recovered interrupted work as unknown instead of retrying automatically.
- Continued batch processing after package-local failures while stopping after backend availability failures.
- Preserved operation outcomes on update rows after inventory refresh.

Release preparation assembles the detailed fragments in `.changes/` into this file. Published history must not be rewritten.
