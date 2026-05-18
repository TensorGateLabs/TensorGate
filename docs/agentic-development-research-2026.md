# Agentic Development Research 2026 - TensorGate

This document captures practical research for your target model:

- heavy agentic execution
- strict human final review
- local-first confidence before remote CI

## Research Highlights

### 1) Branch protection + required checks are non-negotiable

GitHub protected-branch guidance emphasizes stable check names, PR-triggered checks,
and strict status check enforcement on latest SHA.

Implication:

- keep required checks minimal, deterministic, and always reporting
- avoid optional/ambiguous check names for merge policy

### 2) Merge queue is high leverage at scale

GitHub merge queue reduces "rebase + rerun churn" and keeps default branch green.

Implication:

- when contributor volume rises, enable merge queue and include `merge_group` trigger
  in required-check workflows

### 3) Supply-chain posture should include provenance

OpenSSF/SLSA practices recommend signed provenance attestations and verification.

Implication:

- maintain Scorecards + SBOM
- next maturity step: add SLSA provenance generation and verify in release pipeline

### 4) PR structure quality directly affects defect leakage

Consistent PR metadata + testing evidence + bounded scope improves review quality.

Implication:

- keep PR readiness gate enforced
- keep template discipline (issue links + testing evidence)

### 5) Projects v2 GraphQL automation is stable for field sync

Project item field updates require specific field/option IDs and proper token scopes.

Implication:

- keep PAT scope (`repo`, `project`) in `TG_PROJECT_TOKEN`
- treat labels as canonical routing signals for sprint/priority/component/role fields

## Agentic-First Local Shipping Blueprint

### Local gate before push

1. `./scripts/preflight.sh`
2. ensure branch is issue-bound (`feature/<issue>-...` or `fix/<issue>-...`)
3. ensure acceptance criteria/risk checklist are closed in working notes

### PR gate before merge

1. PR body links issue (`Closes #...` / `Relates to #...`)
2. PR Intelligence checklist resolved
3. Traceability Matrix comment has no open gap
4. required checks green on latest SHA
5. human reviewer approves correctness and risk posture

### Post-merge gate

1. evidence log posted back to linked issue
2. if unresolved risk remains, spawn follow-up issue with labels/sprint

## Next-Step Research-Driven Backlog

1. Enable merge queue + `merge_group` support in required workflows.
2. Add SLSA provenance generation/verification to release path.
3. Add flaky-check detector (weekly report from workflow run outcomes).
4. Add risk-based test selection hints (component/label -> test subsets).
