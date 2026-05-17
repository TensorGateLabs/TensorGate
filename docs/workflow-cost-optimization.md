# Workflow Cost Optimization Policy

This policy minimizes GitHub Actions minutes while preserving merge quality.

## Cost Strategy

### Tier 1 - Required per PR (fast, merge-blocking)

- CI (bootstrap guard / build / test / format)
- Branch Name Check
- PR Title Check
- Docs Quality
- Secret Scan (PR scope)

These are the only checks that should gate merges.

### Tier 2 - Async/security depth (scheduled or manual)

- CodeQL (PR + weekly schedule)
- OpenSSF Scorecards (weekly/manual)
- SBOM generation (release/manual)

These provide security depth without burning minutes on every push.

### Tier 3 - Planning automation (event-driven, low runtime)

- Issue Intelligence
- Issue Decomposition
- PR Intelligence
- Traceability Matrix
- Project Automation

These are short metadata workflows and are kept event-based.

## Trigger Policy (implemented)

- Removed `push` triggers from:
  - `ci.yml`
  - `codeql.yml`
  - `docs-quality.yml`
  - `secrets-scan.yml`
  - `release-drafter.yml`
- Replaced `scorecards.yml` push trigger with `workflow_dispatch`.

## Operational Recommendations

1. Avoid direct pushes to `main`; use PR flow exclusively.
2. Keep required checks stable by name.
3. Run scheduled heavy scans during off-hours.
4. Review monthly run volume:

```bash
gh run list --repo syed-dawood/TensorGate --limit 200
```

5. If costs rise, trim first from Tier 2 frequency before touching Tier 1.
