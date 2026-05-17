# TensorGate Operating Pipeline (0% -> 100%)

This runbook defines a deterministic engineering flow for solo and multi-contributor scenarios.

## 1) Intake and Planning

- Source of truth: GitHub issues + project board.
- Every item must have: `component:*`, `type:*`, `priority:*`, `sprint:*`.
- Branch naming must follow:
  - `feature/<issue>-<short-desc>`
  - `fix/<issue>-<short-desc>`
  - `refactor/<short-desc>`
  - `docs/<short-desc>`
  - `bench/<short-desc>`

## 2) Local Development Guardrails

Run before every push:

```bash
./scripts/preflight.sh
```

The script enforces:

- SDK/tooling availability
- restore/build/test/format (when .NET workspace exists)
- bootstrap-safe behavior before first `*.sln`/`*.csproj`

## 3) Pull Request Quality Gates (Remote)

Required checks are implemented as workflows:

- CI (build/test/format)
- Docs Quality
- CodeQL
- Dependency Review
- Secret Scan
- PR Title Check
- Branch Name Check
- PR Labeler
- PR Size Labeler

## 4) Security and Supply Chain

- Dependabot for NuGet + Actions.
- Secret scanning via `gitleaks`.
- OpenSSF Scorecards.
- SBOM generation for releases.
- Dependency graph enabled for dependency review.

## 5) Release and Traceability

- Release drafter keeps release notes continuously updated.
- Tag-based release workflow (`v*.*.*`) builds/tests/packages.
- Keep project ADRs and benchmark artifacts linked from release notes.

## 6) Scenario Playbooks

### New requirement discovered

1. Open issue with labels + sprint.
2. Add acceptance criteria.
3. Link ADR if architecture changes.
4. Implement via feature branch + PR checks.

### Bug found in production/test

1. Open bug issue with reproduction + severity.
2. Label `type:testing` or `bug`, proper `component:*`, `priority:*`, `sprint:*`.
3. Add failing test first.
4. Fix and validate in CI before merge.

### New contributor opens PR

1. Automatic checks run.
2. Labeler + size labeling classify PR.
3. Maintainer verifies issue linkage, test coverage, and docs.
4. Merge only when required checks are green.

### Dependabot PR wave

1. Batch-inspect common failing checks.
2. Fix systemic workflow issues on `main`.
3. Update PR branches and re-run checks.
4. Merge smallest-risk updates first.

## 7) Continuous Improvement Loop

- Weekly:
  - review stale/blocked work
  - triage CI flake patterns
  - tune branch protection required checks
- Sprint close:
  - validate benchmark regressions
  - validate HarmBench/F1 outcomes
  - update release draft and milestone burndown
