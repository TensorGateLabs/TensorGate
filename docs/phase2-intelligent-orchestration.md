# Phase 2 - Intelligent Orchestration

This phase adds semi-automated intelligence across the full software lifecycle.

## Objectives

- Automatically triage and route new issues.
- Automatically prepare PR review and QA plans.
- Enforce PR readiness metadata for human-authored changes.
- Keep local and remote quality gates aligned.

## Orchestration Layers

### Layer A: Issue Intelligence

Workflow: `.github/workflows/issue-intelligence.yml`

- infers labels from issue title/body (type, component, priority)
- applies sprint heuristic (`sprint:1/2/3`)
- posts a triage summary comment for transparency

### Layer A2: Issue Decomposition

Workflow: `.github/workflows/issue-decomposition.yml`

- posts AI-assisted decomposition for new issues:
  - suggested sub-tasks
  - suggested acceptance criteria
  - risk checklist
- avoids duplicate decomposition comments
- can be bypassed with `skip-decomposition` label

### Layer B: Project Routing

Workflow: `.github/workflows/project-automation.yml`

- adds issues/PRs to the project board automatically
- synchronizes Project v2 fields from labels:
  - `Sprint` from `sprint:*`
  - `Priority` from `priority:*`
  - `Component` from `component:*`
  - `Role` from `role:*`
  - `Status` defaults to `Todo`
- uses `TG_PROJECT_TOKEN` (PAT with `project` + `repo` scopes)

### Layer C: PR Intelligence

Workflow: `.github/workflows/pr-intelligence.yml`

- analyzes changed files and produces:
  - review gate checklist
  - QA gate checklist (critical/non-biased)
  - merge readiness checklist
- applies component/documentation labels to PRs

### Layer D: PR Readiness Gate

Workflow: `.github/workflows/pr-readiness-gate.yml`

- enforces issue linkage and testing evidence for human-authored PRs
- skips Dependabot PRs

### Layer E: Local Operator Loop

Scripts:

- `scripts/preflight.sh`: deterministic local build/test/format gate before push

## Standard End-to-End Flow

1. Open issue.
2. Issue intelligence applies labels + sprint.
3. Work on branch using naming policy.
4. Run local preflight.
5. Open PR linked to issue.
6. PR intelligence posts review+QA plan.
7. PR readiness validates metadata.
8. CI/security/docs checks run.
9. Merge only after all required checks are green.

## Verification Commands

```bash
# issue intelligence + routing visibility
gh issue view <issue> --repo TensorGateLabs/TensorGate

# PR checks
gh pr checks <pr> --repo TensorGateLabs/TensorGate

# local preflight
./scripts/preflight.sh
```

## One-Time Setup (Required)

Create repository secret `TG_PROJECT_TOKEN` containing a PAT with:

- `repo`
- `project`

Without this secret, project field synchronization is skipped and the workflow
will emit a setup notice.
