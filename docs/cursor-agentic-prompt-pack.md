# Cursor Agentic Prompt Pack

Use these prompts in Composer/Chat when working an issue end-to-end.

## 1) Full Lifecycle Prompt (default)

```text
Work issue #<N> using PM -> Dev -> QA -> DevOps personas.
PM: restate scope, non-goals, acceptance criteria.
Dev: implement smallest safe change.
QA: run deterministic tests including one edge/failure path.
DevOps: verify CI/workflow impact and traceability artifacts.
Return: findings first, then change summary, then residual risks.
Do not stop at planning; execute and verify.
```

## 2) QA Hard Mode Prompt

```text
Act as skeptical QA lead. Attempt to falsify the implementation.
Generate critical test cases (happy path, edge, failure, regression).
Run what is possible locally and report concrete evidence.
If confidence is insufficient, fail with explicit blockers.
```

## 3) CI Cost-Aware Prompt

```text
Optimize this change for minimal CI minutes while preserving merge safety.
Avoid adding push-triggered heavy workflows.
Prefer PR-only checks and scheduled security scans for heavy jobs.
Show trigger changes and why they reduce cost.
```

## 4) Traceability Prompt

```text
Ensure requirement-to-merge traceability for this change.
Link issue in PR body, verify traceability matrix presence, and confirm
acceptance criteria + risk checklist are closed with evidence.
```
