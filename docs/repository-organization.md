# Repository Organization (TensorGateLabs)

TensorGate lives under the **[TensorGateLabs](https://github.com/TensorGateLabs)** GitHub organization:

- **Repository:** https://github.com/TensorGateLabs/TensorGate
- **Discussions:** https://github.com/TensorGateLabs/TensorGate/discussions
- **Security advisories:** https://github.com/TensorGateLabs/TensorGate/security/advisories

## Project board

Sprint execution is tracked on the v0.1 project board (currently under the founder account, linked to this repo):

- https://github.com/users/syed-dawood/projects/1

`project-automation.yml` uses that board URL and `user: syed-dawood` for field sync. When the board is migrated to an org project, update:

1. `project-url` in `.github/workflows/project-automation.yml`
2. `user:` → `organization: TensorGateLabs` (and `project_number`) on all `update-project-action` steps
3. This doc and the README project board link

## Local clone remote

```bash
git remote set-url origin https://github.com/TensorGateLabs/TensorGate.git
```

## CLI default repo slug

Scripts and docs assume `TensorGateLabs/TensorGate` unless `GITHUB_REPOSITORY` is set (e.g. in Actions).
