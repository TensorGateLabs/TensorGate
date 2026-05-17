# Phase 2.3 - Traceability and Evidence Matrix

This phase closes the requirement-to-merge loop with automated evidence.

## What it adds

Workflow: `.github/workflows/traceability-matrix.yml`

### A) PR Traceability Matrix (during PR lifecycle)

- Trigger: PR opened/edited/synchronized/reopened
- Parses linked issue references from PR body (`Closes #...`, `Relates to #...`)
- Posts/updates a PR comment containing:
  - requirement source
  - implementation pointer
  - verification pointer
  - explicit gap check

### B) Post-Merge Evidence Log (after merge)

- Trigger: PR merged
- Posts merge evidence back to linked issue(s):
  - merged PR reference
  - merge commit SHA
  - timestamp
  - verification trail note

## Why it matters

- Creates auditable proof from issue intake to merged artifact.
- Reduces ambiguity in human/manual review.
- Makes follow-up risk/gap tracking explicit and durable.

## Operational rule

Always include issue links in PR body:

- `Closes #123`
- `Relates to #123`

Without linked issues, traceability matrix will flag a gap.
