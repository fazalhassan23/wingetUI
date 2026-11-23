# Contributing to WingetUI

WingetUI is a Windows 11 WPF application built with .NET 10 and the WinGet COM API. Contributions should preserve existing behavior unless the change fixes a documented defect or intentionally introduces an approved feature.

## Before starting

1. Read [the development guide](docs/development.md) and [architecture guide](docs/architecture.md).
2. Check existing issues and pull requests.
3. Choose the correct base branch using [the Git workflow](docs/git-workflow.md).
4. Keep unrelated local changes out of the branch.

## Engineering expectations

- Keep domain rules in `WingetUI.Core` and free of WPF, filesystem, and WinGet COM dependencies.
- Keep App Installer COM types inside `WingetUI.WinGet`.
- Keep confirmation dialogs and other user interaction in the presentation layer.
- Treat the operation queue as the source of truth for package activity.
- Validate external state again immediately before an install or upgrade.
- Do not automatically retry interrupted package operations.
- Never log or commit credentials, tokens, private package metadata, or personal activity history.

## Validation

Run the deterministic checks and builds described in [testing](docs/testing.md). Run the live smoke test when changing WinGet activation, catalog access, package mapping, or target validation. The smoke test must not install or upgrade packages.

Run `./tools/Validate-Documentation.ps1` after changing Markdown. It checks the required documentation set, relative links, and fenced code blocks.

User-visible changes require a fragment in `.changes/`. Name it for the issue or change and use one of these suffixes:

- `.added.md`
- `.changed.md`
- `.fixed.md`
- `.deprecated.md`
- `.removed.md`
- `.security.md`

## Pull requests

Explain the problem, implementation, validation, documentation impact, compatibility risks, and any checks that were not performed. Link genuine defects with `Closes #123` only when merging the pull request should close the issue.

Do not include generated `bin`, `obj`, `.local`, published ZIP, or local activity-history files.
