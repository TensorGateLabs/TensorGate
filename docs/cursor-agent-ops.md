# Cursor Agent and MCP Operations Matrix

This document defines how to use Cursor-native agents and MCP tools consistently for TensorGate.

## Recommended Agent Routing

| Scenario | Primary Agent/Skill | Outcome |
|---|---|---|
| Broad codebase discovery | `explore` subagent | Fast readonly mapping |
| Single CI failure diagnosis | `ci-investigator` | Root cause summary |
| Continuous CI watching | `ci-watcher` / `loop-on-ci` skill | Fix-until-green loop |
| PR lifecycle babysitting | `babysit` skill | Merge-ready PR |
| Startup reliability check | `startup-review` | Cold-start setup report |
| Docs setup reliability | `docs-reliability-review` | Correct setup docs path |
| Validation strategy quality | `validation-review` | Efficient verification path |

## MCP Usage Rules

- Use `user-github`/`gh` as source of truth for PRs/checks/issues.
- Use `plugin-context7-plugin-context7` for current library/framework docs.
- Use `cursor-app-control` only when workspace migration or project creation is needed.
- Prefer repository-local automation first; MCP augments, not replaces, CI policy.

## Standard Execution Sequence

1. **Research**
   - Gather active issue/PR/check state from GitHub.
   - Pull current library docs for any uncertain API/workflow behavior.
2. **Plan**
   - Define minimal safe change and verification points.
3. **Execute**
   - Apply code/workflow/config changes.
4. **Verify**
   - Local preflight + remote check status.
5. **Close**
   - Update issue/PR with outcomes and next action.

## CI Failure Runbook

1. `gh pr checks <pr-number>`
2. inspect first actionable failure (`gh run view <run-id> --log-failed`)
3. smallest safe fix
4. push and re-check
5. repeat until all required checks pass
