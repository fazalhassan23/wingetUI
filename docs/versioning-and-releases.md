# Versioning and releases

WingetUI will use Semantic Versioning: `MAJOR.MINOR.PATCH`.

- MAJOR: incompatible user workflow, data-format, or supported-platform changes.
- MINOR: backward-compatible features.
- PATCH: backward-compatible fixes.

During `0.x`, the product is a preview and compatibility may evolve, but incompatible changes still require explicit documentation and migration guidance.

## Current status

Version `0.1.0` is the first development-preview release. `Directory.Build.props` is the authoritative release-version source; assembly and file metadata derive from it. The immutable release tag is `v0.1.0`.

The 0.1.0 binaries are unsigned because the project has no verified code-signing certificate. Release notes and download guidance must state this limitation and provide SHA-256 checksums.

CI artifacts remain development outputs unless they are explicitly attached to a tagged GitHub release.

## Release preparation

1. Create a release-preparation branch from staging.
2. Assemble `.changes` fragments into `CHANGELOG.md`.
3. Set the version in the authoritative source.
4. Review supported Windows and App Installer versions.
5. Update compatibility, limitations, migration, and recovery guidance.
6. Run deterministic, smoke, publish, formatting, documentation, and selected manual checks.
7. Verify the ZIP on a clean Windows account without an installed SDK.
8. Create a pull request into staging with release notes and risks.

Integration from staging to main, creating a tag, publishing a release, and distributing binaries are separate actions requiring explicit authorization. Release tags must be immutable.
