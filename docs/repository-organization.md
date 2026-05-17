# Repository Organization (TensorGateLabs)

TensorGate lives under the **[TensorGateLabs](https://github.com/TensorGateLabs)** GitHub organization:

- **Repository:** https://github.com/TensorGateLabs/TensorGate
- **Discussions:** https://github.com/TensorGateLabs/TensorGate/discussions
- **Security advisories:** https://github.com/TensorGateLabs/TensorGate/security/advisories
- **Project board:** https://github.com/orgs/TensorGateLabs/projects/1
- **Maintainers team:** https://github.com/orgs/TensorGateLabs/teams/maintainers

## Project board

Sprint execution is tracked on the org project **TensorGate v0.1 — AI Safety Middleware**:

- https://github.com/orgs/TensorGateLabs/projects/1

Automation (`.github/workflows/project-automation.yml`) uses:

- `project-url`: `https://github.com/orgs/TensorGateLabs/projects/1`
- `organization: TensorGateLabs` + `project_number: 1` for field sync
- Secret: `TG_PROJECT_TOKEN` (PAT with `repo` + `project` scopes)

The previous founder user project (`syed-dawood/projects/1`) is superseded; issues are linked on the org board.

## Teams

| Team | Purpose |
|------|---------|
| `@TensorGateLabs/maintainers` | Default CODEOWNERS, review routing, `maintain` on this repo |

Add members: **Org Settings → Teams → maintainers → Members**.

## Local clone remote

```bash
git remote set-url origin https://github.com/TensorGateLabs/TensorGate.git
```

## CLI default repo slug

Scripts resolve `TensorGateLabs/TensorGate` via `scripts/github-repo.sh` (git `origin`, `GITHUB_REPOSITORY`, or default).
