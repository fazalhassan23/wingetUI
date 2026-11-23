# Versioning and releases

WingetUI will use Semantic Versioning: `MAJOR.MINOR.PATCH`.

- MAJOR: incompatible user workflow, data-format, or supported-platform changes.
- MINOR: backward-compatible features.
- PATCH: backward-compatible fixes.

During `0.x`, the product is a preview and compatibility may evolve, but incompatible changes still require explicit documentation and migration guidance.

## Current status

No stable version, immutable release tag, or published GitHub release is currently documented. CI artifacts are development outputs and are not releases.

Before the first release, establish one authoritative version source in the build, derive display and package metadata from it, and document any prerelease suffix.

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
