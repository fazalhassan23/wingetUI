# Git and integration workflow

## Branch roles

- `staging` integrates regular development.
- `main` contains stable integrations.
- Focused working branches contain one cohesive change.

Regular work starts from the latest `origin/staging`, uses a feature, refactor, documentation, or chore branch, and enters staging through a pull request.

A verified production defect starts from `origin/main`, uses a fix branch, and targets main. After merge, propagate the equivalent fix to staging through a separate pull request when needed.

Work that depends on an unmerged feature may target that feature branch first. The dependency must be documented, and the parent pull request remains responsible for integration into staging.

## Commits

Use focused Conventional Commit messages when no narrower repository convention applies:

```text
feat(ui): add package filtering
fix(queue): retain pending operations
refactor(winget): isolate catalog access
docs: add troubleshooting guide
```

Use the configured developer identity. Do not rewrite published history or force-push without explicit authorization.

## Pull requests

Every independently reviewable change should have a pull request describing summary, related issue, implementation, validation, documentation impact, and compatibility risks. Verify the base branch immediately before merge.

Regular development does not enter `main` without explicit release authorization. Verified, narrowly scoped defect fixes may follow the authorized issue-to-resolution process documented in `AGENTS.md`.

## Release integration

Prepare releases through a dedicated branch into staging. Moving staging to main requires an explicitly authorized pull request. Tagging, publishing GitHub releases or packages, and deployment require separate authorization.
