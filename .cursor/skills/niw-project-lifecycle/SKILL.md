---
name: niw-project-lifecycle
description: >-
  Mandatory NIW-grade issue lifecycle for TensorGate: board transitions, assignee,
  research-first dev, PR, peer review, QA, merge, and audit trail. Use before
  starting ANY board issue. Simulates PM/Architect/Dev/Review/QA/DevOps via
  GitHub history only (no role labels in comments).
---

# TensorGate NIW Project Lifecycle

**Trigger:** Before implementing any GitHub issue from the project board.

**Repo:** `TensorGateLabs/TensorGate`  
**Board:** https://github.com/orgs/TensorGateLabs/projects/1  
**Assignee (all steps):** `syed-dawood` (single profile; simulate team via board + professional comments only).

## Rules

1. **Never** write "acting as QA" / "DevOps mode" in GitHub text — use natural PM/dev/review/QA language.
2. **Always** update project **Status** in order (skip only with a comment explaining why).
3. **Always** link PR with `Closes #N` or `Relates to #N`.
4. **Research → plan → execute** per phase; record plans in issue/PR comments.
5. If review or QA fails, move board **backward** (e.g. Ready For QA → In Progress), open follow-up tasks or request changes on PR.
6. Deployment: if no pipeline exists, comment on issue that release is deferred and what would be required.

## Status order (default happy path)

| Step | Board Status | GitHub actions |
|------|----------------|----------------|
| 1 | **Todo** | Assign issue; PM comment: scope, AC, risks |
| 2 | **In Progress** | Dev research comment; branch `feature/<N>-<slug>` |
| 3 | **Peer Review** | Open PR; review checklist comment on PR |
| 4 | **Ready For QA** | Address review; QA test plan comment on PR |
| 5 | **QA Complete** | QA evidence comment; merge PR |
| 6 | **Done** | Close issue; merge evidence on issue; DevOps/deployment note |

## Persona checklists (internal — do not paste role names)

### PM / intake
- Restate acceptance criteria and non-goals
- Confirm labels/sprint/component on issue
- Assign `@syed-dawood`

### Architect / dev (research first)
- Read issue + references; note FSM/API boundaries
- Post short research comment on issue before coding
- Minimal diff; match `Directory.Build.props` conventions

### Peer reviewer
- Read full diff; security, perf, tests, naming
- PR comment: blocking vs non-blocking findings
- If blocking: move board to **In Progress**, do not merge

### QA
- Happy path + edge + regression tests locally
- PR comment with commands run and results
- If fail: move board back, file bug or request changes

### DevOps
- Note CI/workflow impact; deployment status
- Post-merge evidence on linked issue

## Commands

```bash
# Board status (see scripts/project-board-transition.sh)
./scripts/project-board-transition.sh <issue-number> "<Status Name>"

# Assign
gh issue edit <N> --repo TensorGateLabs/TensorGate --add-assignee syed-dawood

# Branch + PR
git checkout -b feature/<N>-<short-desc>
./scripts/preflight.sh
gh pr create --repo TensorGateLabs/TensorGate --base main --head feature/<N>-<short-desc> \
  --title "feat: <title> (#<N>)" \
  --body "$(cat <<EOF
## Summary
...

Closes #<N>

## Testing evidence
- [ ] Unit tests added/updated
- [ ] Manual testing performed
- [ ] Local preflight passed
EOF
)"
```

## PR body requirements (human gate when CI unfrozen)

- `Closes #N`
- Testing checklist checked with evidence
- Risk / rollback note for non-trivial changes
