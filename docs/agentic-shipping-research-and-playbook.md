# Agentic Shipping Research and Playbook

This document defines a practical, high-assurance approach for agentic development with mandatory human review.

## Goal

Ship high-quality changes that are "merge-ready before leaving local" by combining:

- agentic planning/execution
- deterministic local gates
- strict remote CI/security gates
- explicit review + QA decomposition

## Research Synthesis

### What usually breaks agentic flows

1. Ambiguous task intake -> wrong scope.
2. Hidden assumptions -> tests pass locally, fail remotely.
3. Over-broad edits -> hard-to-review PRs and regressions.
4. Missing acceptance criteria -> endless iteration loops.
5. Inconsistent local/remote checks -> "works on my machine" drift.

### What consistently works

1. Strong issue intake schema and automated triage/routing.
2. Small bounded PRs with explicit linked issue + acceptance criteria.
3. Local gate mirroring remote checks before push.
4. Required branch protection checks for objective merge criteria.
5. Human final review focused on correctness/risk, not formatting/noise.

## Agentic Operating Model (Recommended)

### Stage 0: Intake

- Use issue templates only (blank issues disabled).
- Let Issue Intelligence apply labels/sprint.
- Let Issue Decomposition propose:
  - sub-tasks
  - acceptance criteria
  - risk checklist

### Stage 1: Research and plan

- Map affected modules and dependencies before editing.
- Verify library/framework behavior against current upstream docs when uncertain.
- Record "what will change / what will not change" before coding.

### Stage 2: Implement

- Use one branch per issue.
- Keep change set focused and minimal.
- Write tests with each behavior change.

### Stage 3: Local verification (must pass before push)

```bash
./scripts/preflight.sh
```

### Stage 4: PR intelligence and review

- PR Intelligence posts review and QA checklist automatically.
- PR Readiness Gate enforces issue linkage and test evidence.
- Human reviewer verifies:
  - correctness
  - edge cases
  - risk checklist closure

### Stage 5: Merge and post-merge

- Merge only when required checks pass.
- If regression appears, create bug issue with regression test requirement.

## Tooling Roles

- `gh` / GitHub API: source of truth for issue/PR/check state.

## Communication pattern for issues and PRs

Use this prompt shape for consistent quality:

1. **Context:** issue link + constraints + non-goals.
2. **Research asks:** files, contracts, dependencies.
3. **Plan asks:** bounded steps + explicit risks.
4. **Execution asks:** minimal diff, no unrelated changes.
5. **Verification asks:** exact commands and expected outcomes.
6. **Output asks:** findings first, then summary and next steps.

## "Perfect Before Push" Checklist

- [ ] Issue linked and scope bounded.
- [ ] Acceptance criteria are explicit and testable.
- [ ] Risks identified with mitigation.
- [ ] Local preflight passed.
- [ ] Diff is minimal and reviewable.
- [ ] PR template completed with evidence.

## Practical Limits

No pipeline can guarantee mathematical perfection; the goal is engineered reliability:

- reduce unknowns early
- detect defects before merge
- preserve traceable decisions and evidence
